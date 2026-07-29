using DiscordStreamNotifyBot.DataBase.Table;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    public partial class YoutubeMemberService
    {
        //https://github.com/member-gentei/member-gentei/blob/90f62385f554eb4c02ed8732e15061b9dd1dd6d0/gentei/membership/membership.go#L331
        //https://discord.com/channels/@me/userChannel.Id
        public async Task CheckMemberShip(object stats)
        {
            bool isOldCheck = (bool)stats;
            YoutubeMemberCheckType checkType = isOldCheck ? YoutubeMemberCheckType.Old : YoutubeMemberCheckType.New;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await CheckMemberShipCore(isOldCheck);
                _metrics.RecordYoutubeMemberCheckCycle(checkType, YoutubeMemberCheckCycleResult.Success);
            }
            catch
            {
                _metrics.RecordYoutubeMemberCheckCycle(checkType, YoutubeMemberCheckCycleResult.Failure);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _metrics.ObserveYoutubeMemberCheckDuration(checkType, stopwatch.Elapsed);
            }
        }

        private async Task CheckMemberShipCore(bool isOldCheck)
        {
            YoutubeMemberCheckType checkType = isOldCheck ? YoutubeMemberCheckType.Old : YoutubeMemberCheckType.New;
            int totalCheckMemberCount = 0, totalIsMemberCount = 0;
            List<GuildYoutubeMemberConfig> needCheckList;

            using (var db = _dbService.GetDbContext())
            {
                needCheckList = await db.GuildYoutubeMemberConfig
                    .AsNoTracking()
                    .Where((x) => !string.IsNullOrEmpty(x.MemberCheckChannelId) && !string.IsNullOrEmpty(x.MemberCheckChannelTitle) && x.MemberCheckVideoId != "-")
                    .ToListAsync();
            }

            Log.Info((isOldCheck ? "舊" : "新") + $"會限檢查開始: {needCheckList.Count()} 個頻道");

            // 舊會限三日分桶（沿用原 splitDay）：降低每日 API 配額，且以 guild 為單位保持驗證一致。
            // 取代原 MemberCheck.dat 檔案游標（本機狀態、對未過濾全表切片、rescale 後失同步）。
            const int SplitDay = 3;

            HashSet<string> checkedMemberSet = new();
            foreach (var guildYoutubeMemberConfig in needCheckList)
            {
                // ① 分片：只處理本 shard 持有的 guild（省掉對別 shard config 的無謂 DB 查詢）
                if (!Bot.IsServerOnThisShard(guildYoutubeMemberConfig.GuildId))
                    continue;

                // ② 舊檢查以 guild 為單位做三日分桶：同一 guild 恆落同一天一起驗證（避免不一致），
                //    每天約 1/3 guild 被舊檢查，等價原三日分批但無狀態、rescale 友善（新 shard 沿用同一桶日）
                if (isOldCheck &&
                    (int)(guildYoutubeMemberConfig.GuildId % SplitDay) != DateTime.Now.DayOfYear % SplitDay)
                    continue;

                using var db = _dbService.GetDbContext();

                var list = await db.YoutubeMemberCheck
                    .Where((x) => x.GuildId == guildYoutubeMemberConfig.GuildId && x.CheckYTChannelId == guildYoutubeMemberConfig.MemberCheckChannelId)
                    .Where((x) => (isOldCheck && x.IsChecked) || (!isOldCheck && !x.IsChecked))
                    .ToListAsync();

                if (list.Count == 0)
                    continue;

                int totalCheckCount = list.Count;

                var guildConfig = await db.GuildConfig.FirstOrDefaultAsync((x) => x.GuildId == guildYoutubeMemberConfig.GuildId);
                if (guildConfig == null)
                {
                    db.GuildConfig.Add(new GuildConfig() { GuildId = guildYoutubeMemberConfig.GuildId });
                    Log.Warn($"{guildYoutubeMemberConfig.GuildId} Guild 不存在於資料庫內");
                    continue;
                }

                var guild = _client.GetGuild(guildYoutubeMemberConfig.GuildId);
                if (guild == null)
                {
                    // 不屬於本 Shard 或尚未 Ready，靜默略過，別刪設定（避免多 Shard 互刪）
                    if (!Bot.ShouldDeleteMissingGuild(guildYoutubeMemberConfig.GuildId))
                        continue;

                    Log.Warn($"{guildYoutubeMemberConfig.GuildId} Guild 不存在");
                    db.GuildYoutubeMemberConfig.RemoveRange(db.GuildYoutubeMemberConfig.Where((x) => x.GuildId == guildYoutubeMemberConfig.GuildId));
                    continue;
                }

                string guildLocale = await _guildLocaleService.GetAsync(guild.Id, guild);
                string setCheckVideoPath = _commandDisplayResolver.GetCommandPath(guildLocale,
                    "member-set", "set-check-video");

                var logChannel = guild.GetTextChannel(guildConfig.LogMemberStatusChannelId);
                if (logChannel == null)
                {
                    Log.Warn($"{guildYoutubeMemberConfig.GuildId} 無紀錄頻道");
                    db.GuildYoutubeMemberConfig.RemoveRange(db.GuildYoutubeMemberConfig.Where((x) => x.GuildId == guildYoutubeMemberConfig.GuildId));
                    continue;
                }

                var role = guild.GetRole(guildYoutubeMemberConfig.MemberCheckGrantRoleId);
                if (role == null)
                {
                    string channelUrl = Format.Url(guildYoutubeMemberConfig.MemberCheckChannelId,
                        $"https://www.youtube.com/channel/{guildYoutubeMemberConfig.MemberCheckChannelId}");
                    await logChannel.SendMessageAsync(_localizer.Format("Member.Status.RoleMissing", guildLocale, channelUrl));
                    Log.Warn($"{guildYoutubeMemberConfig.GuildId} / {guildYoutubeMemberConfig.MemberCheckChannelId} RoleId 不存在 {guildYoutubeMemberConfig.MemberCheckGrantRoleId}");
                    db.GuildYoutubeMemberConfig.Remove(guildYoutubeMemberConfig);
                    continue;
                }

                var permission = guild.CurrentUser.GetPermissions(logChannel);
                if (!permission.ViewChannel || !permission.SendMessages || !permission.EmbedLinks)
                {
                    Log.Warn($"{guildYoutubeMemberConfig.GuildId} / {guildConfig.LogMemberStatusChannelId} 無權限可紀錄");
                    db.GuildYoutubeMemberConfig.Remove(guildYoutubeMemberConfig);
                    continue;
                }

                if (!guild.CurrentUser.GuildPermissions.ManageRoles)
                {
                    await logChannel.SendMessageAsync(_localizer.Get("Member.Status.ManageRolesMissing", guildLocale));
                    Log.Warn($"{guildYoutubeMemberConfig.GuildId} 無權限可給予用戶組");
                    continue;
                }

                if (role == guild.EveryoneRole)
                {
                    Log.Warn($"{guildYoutubeMemberConfig.GuildId} / {guildYoutubeMemberConfig.MemberCheckChannelId} 設定成 everoyne 用戶組");
                    await logChannel.SendMessageAsync(_localizer.Get("Member.Status.EveryoneRoleInvalid", guildLocale));
                    db.GuildYoutubeMemberConfig.Remove(guildYoutubeMemberConfig);
                    continue;
                }

                int checkedMemberCount = 0;
                foreach (var member in list)
                {
                    totalCheckMemberCount++;
                    string userLocale = _localeResolver.ResolveDelayedDirectMessage(member.Locale, guildLocale);
                    string checkPath = _commandDisplayResolver.GetCommandPath(userLocale, "member", "check");
                    string cancelPath = _commandDisplayResolver.GetCommandPath(userLocale, "member", "cancel-member-check");
                    string showAccountPath = _commandDisplayResolver.GetCommandPath(userLocale, "member", "show-my-youtube-account");
                    string website = Format.Url(_localizer.Get("Common.Website", userLocale), "https://stream-bot.konnokai.me/");
                    string googleSecurity = Format.Url(_localizer.Get("Common.GoogleSecurity", userLocale),
                        "https://myaccount.google.com/permissions?continue=https%3A%2F%2Fmyaccount.google.com%2Fsecurity");
                    if (!checkedMemberSet.Contains($"{member.UserId}-{member.CheckYTChannelId}"))
                    {
                        var token = await flow.LoadTokenAsync(member.UserId.ToString(), CancellationToken.None);
                        if (token == null)
                        {
                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.TokenMissing);
                            await RemoveMemberCheckFromDbAsync(member.UserId);

                            await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                _localizer.Get("Member.Status.NotLoggedIn", guildLocale), _localizer, guildLocale);
                            await member.UserId.SendErrorMessageAsync(_client,
                                _localizer.Format("Member.Background.LoginRequired", userLocale, website, checkPath),
                                logChannel, _localizer, guildLocale);

                            continue;
                        }

                        UserCredential userCredential = null;
                        try
                        {
                            userCredential = await GetUserCredentialAsync(member.UserId.ToString(), token);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message == "RefreshToken 空白")
                            {
                                _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.RefreshTokenMissing);
                                await RevokeUserGoogleCertAsync(member.UserId.ToString());

                                await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                    guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                    _localizer.Get("Member.Status.RefreshFailed", guildLocale), _localizer, guildLocale);
                                await member.UserId.SendErrorMessageAsync(_client,
                                    _localizer.Format("Member.Background.RefreshFailed", userLocale,
                                        googleSecurity, website, checkPath), logChannel, _localizer, guildLocale);

                                continue;
                            }

                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.UnknownError);
                            Log.Error(ex.ToString());
                            continue;
                        }

                        if (userCredential == null)
                        {
                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.CredentialExpired);
                            await RemoveMemberCheckFromDbAsync(member.UserId);

                            await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                _localizer.Get("Member.Status.CredentialExpired", guildLocale), _localizer, guildLocale);
                            await member.UserId.SendErrorMessageAsync(_client,
                                _localizer.Format("Member.Background.CredentialExpired", userLocale,
                                    googleSecurity, website, checkPath), logChannel, _localizer, guildLocale);

                            continue;
                        }

                        if (guildYoutubeMemberConfig.MemberCheckVideoId == "-")
                            break;

                        var service = new YouTubeService(new BaseClientService.Initializer()
                        {
                            HttpClientInitializer = userCredential,
                            ApplicationName = "Discord Youtube Member Check"
                        }).CommentThreads.List("id");
                        service.VideoId = guildYoutubeMemberConfig.MemberCheckVideoId;

                        bool isMember = false;
                        try
                        {
                            await service.ExecuteAsync().ConfigureAwait(false);
                            isMember = true;
                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.Member);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                if (ex.Message.ToLower().Contains("parameter has disabled comments")) // Todo: 這邊可能需要在抓取新影片後重新驗證會限
                                {
                                    _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.CommentsDisabled);
                                    Log.Warn($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 的會限資格取得失敗");
                                    Log.Warn($"{guildYoutubeMemberConfig.MemberCheckChannelTitle} ({guildYoutubeMemberConfig.MemberCheckChannelId}): {guildYoutubeMemberConfig.MemberCheckVideoId}已關閉留言");
                                    await Bot.ApplicatonOwner.Id.SendErrorMessageAsync(_client, $"{guildYoutubeMemberConfig.GuildId} - {member.UserId} 會限資格取得失敗: {guildYoutubeMemberConfig.MemberCheckVideoId}已關閉留言", logChannel);

                                    // 手動 pin 的探測影片失效：videoId 設 "-" 暫停驗證，但保留 IsManualVideoId（Scraper 不會自動重挑高階影片），通知管理員重設
                                    if (guildYoutubeMemberConfig.IsManualVideoId)
                                    {
                                        try
                                        {
                                            await logChannel.SendMessageAsync(_localizer.Format(
                                                "Member.Status.ManualVideoCommentsDisabled", guildLocale,
                                                guildYoutubeMemberConfig.MemberCheckVideoId, setCheckVideoPath));
                                        }
                                        catch { }
                                    }

                                    guildYoutubeMemberConfig.MemberCheckVideoId = "-";
                                    db.GuildYoutubeMemberConfig.Update(guildYoutubeMemberConfig);
                                    await db.SaveChangesAsync();

                                    break;
                                }
                                else if (ex.Message.ToLower().Contains("notfound"))
                                {
                                    _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.VideoNotFound);
                                    Log.Warn($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 的會限資格取得失敗");
                                    Log.Warn($"{guildYoutubeMemberConfig.MemberCheckChannelTitle} ({guildYoutubeMemberConfig.MemberCheckChannelId}): {guildYoutubeMemberConfig.MemberCheckVideoId} 已刪除影片");
                                    await Bot.ApplicatonOwner.Id.SendErrorMessageAsync(_client, $"{guildYoutubeMemberConfig.GuildId} - {member.UserId} 會限資格取得失敗: {guildYoutubeMemberConfig.MemberCheckVideoId} 已刪除影片", logChannel);

                                    // 手動 pin 的探測影片失效：videoId 設 "-" 暫停驗證，但保留 IsManualVideoId（Scraper 不會自動重挑高階影片），通知管理員重設
                                    if (guildYoutubeMemberConfig.IsManualVideoId)
                                    {
                                        try
                                        {
                                            await logChannel.SendMessageAsync(_localizer.Format(
                                                "Member.Status.ManualVideoDeleted", guildLocale,
                                                guildYoutubeMemberConfig.MemberCheckVideoId, setCheckVideoPath));
                                        }
                                        catch { }
                                    }

                                    guildYoutubeMemberConfig.MemberCheckVideoId = "-";
                                    db.GuildYoutubeMemberConfig.Update(guildYoutubeMemberConfig);
                                    await db.SaveChangesAsync();

                                    break;
                                }
                                else if (!ex.Message.ToLower().Contains("quotaexceeded") &&
                                    (ex.Message.ToLower().Contains("403") || ex.Message.ToLower().Contains("unauthorized") ||
                                    ex.Message.ToLower().Contains("the request might not be properly authorized") || ex.Message.ToLower().Contains("forbidden")))
                                {
                                    _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.NotMember);
                                    Log.Warn($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 的會限資格取得失敗: 無會員");

                                    db.YoutubeMemberCheck.Remove(member);

                                    try
                                    {
                                        await _client.Rest.RemoveRoleAsync(guild.Id, member.UserId, role.Id).ConfigureAwait(false);
                                        _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Remove, YoutubeMemberRoleResult.Success);
                                    }
                                    catch (Discord.Net.HttpException discordEx) when (discordEx.DiscordCode.Value == DiscordErrorCode.UnknownAccount ||
                                        discordEx.DiscordCode.Value == DiscordErrorCode.UnknownMember ||
                                        discordEx.DiscordCode.Value == DiscordErrorCode.UnknownUser)
                                    {
                                        _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Remove, YoutubeMemberRoleResult.UserMissing);
                                        Log.Warn($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 該會員已離開伺服器");
                                        continue;
                                    }
                                    catch (Discord.Net.HttpException discordEx) when (discordEx.DiscordCode == DiscordErrorCode.MissingPermissions)
                                    {
                                        _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Remove, YoutubeMemberRoleResult.MissingPermission);
                                        Log.Warn($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 缺少權限，無法移除用戶組");
                                        await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                            guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                            _localizer.Get("Member.Status.RemoveRolePermissionMissing", guildLocale),
                                            _localizer, guildLocale);
                                        continue;
                                    }
                                    catch (Exception ex2)
                                    {
                                        _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Remove,
                                            ex2 is Discord.Net.HttpException ? YoutubeMemberRoleResult.DiscordError : YoutubeMemberRoleResult.UnknownError);
                                        Log.Error(ex2, $"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 無法移除用戶組");
                                    }

                                    if (isOldCheck)
                                    {
                                        await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                            guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                            _localizer.Get("Member.Status.MembershipExpired", guildLocale), _localizer, guildLocale);
                                        await member.UserId.SendErrorMessageAsync(_client,
                                            _localizer.Format("Member.Background.MembershipExpired", userLocale,
                                                guild.Name, guildYoutubeMemberConfig.MemberCheckChannelTitle, cancelPath, checkPath),
                                            logChannel, _localizer, guildLocale);
                                    }
                                    else
                                    {
                                        await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                            guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                            _localizer.Get("Member.Status.NotMember", guildLocale), _localizer, guildLocale);
                                        await member.UserId.SendErrorMessageAsync(_client,
                                            _localizer.Format("Member.Background.NotMember", userLocale,
                                                guild.Name, guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                                showAccountPath, cancelPath, Bot.ApplicatonOwner),
                                            logChannel, _localizer, guildLocale);
                                    }
                                    continue;
                                }
                                else if (ex.Message.ToLower().Contains("token has been expired or revoked") ||
                                    ex.Message.ToLower().Contains("the access token has expired and could not be refreshed") ||
                                    ex.Message.ToLower().Contains("authenticateduseraccountclosed") || ex.Message.ToLower().Contains("authenticateduseraccountsuspended") ||
                                    ex.Message.ToLower().Contains("user is suspended")) // 帳號被 Google 停用
                                {
                                    _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.CredentialExpired);
                                    Log.Warn($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 的會限資格取得失敗: AccessToken 已過期或無法刷新");
                                    Log.Warn(JsonConvert.SerializeObject(userCredential.Token));
                                    Log.Warn(ex.ToString());

                                    await RemoveMemberCheckFromDbAsync(member.UserId);

                                    await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                        guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                        _localizer.Get("Member.Status.CredentialExpired", guildLocale), _localizer, guildLocale);
                                    await member.UserId.SendErrorMessageAsync(_client,
                                        _localizer.Format("Member.Background.CredentialExpired", userLocale,
                                            googleSecurity, website, checkPath), logChannel, _localizer, guildLocale);
                                    continue;
                                }
                                else if (ex.Message.ToLower().Contains("the added or subtracted value results in an un-representable"))
                                {
                                    _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.CredentialExpired);
                                    Log.Error($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 的會限資格取得失敗: 時間加減錯誤");
                                    Log.Error(ex.ToString());

                                    await RevokeUserGoogleCertAsync(member.UserId.ToString());

                                    await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                        guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                        _localizer.Get("Member.Status.TimeCalculationError", guildLocale), _localizer, guildLocale);
                                    await member.UserId.SendErrorMessageAsync(_client,
                                        _localizer.Format("Member.Background.RetryLogin", userLocale,
                                            googleSecurity, website, checkPath), logChannel, _localizer, guildLocale);
                                    continue;
                                }
                                else if (ex.Message.ToLower().Contains("500") || ex.Message.ToLower().Contains("badgateway") || ex.Message.ToLower().Contains("internalservererror"))
                                {
                                    _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.Provider5xx);
                                    Log.Error($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 的會限資格取得失敗: 500 內部錯誤");

                                    await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                        guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                        _localizer.Get("Member.Status.GoogleInternalError", guildLocale), _localizer, guildLocale);
                                    continue;
                                }
                                else if (ex.Message.ToLower().Contains("bad req") || ex.Message.ToLower().Contains("badrequest"))
                                {
                                    _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.Provider4xx);
                                    Log.Error($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 的會限資格取得失敗: 400 錯誤");

                                    await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                        guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                        _localizer.Get("Member.Status.GoogleBadRequest", guildLocale), _localizer, guildLocale);
                                    continue;
                                }
                                else if (ex.Message.ToLower().Contains("quotaexceeded"))
                                {
                                    _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.QuotaExceeded);
                                    Log.Error($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 的會限資格取得失敗: 無 API 配額");

                                    await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                        guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                        _localizer.Get("Member.Status.QuotaExceeded", guildLocale), _localizer, guildLocale);
                                    break;
                                }
                                else if (ex.Message.ToLower().Contains("resource temporarily unavailable"))
                                {
                                    _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.TemporaryFailure);
                                    Log.Error($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 的會限資格取得失敗: 暫時無法存取資源");
                                    continue;
                                }
                                else
                                {
                                    _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.UnknownError);
                                    Log.Error($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 的會限資格取得失敗: 未知的錯誤");
                                    Log.Error(ex.ToString());

                                    await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                        guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                        _localizer.Get("Member.Status.UnknownError", guildLocale), _localizer, guildLocale);
                                    await member.UserId.SendErrorMessageAsync(_client,
                                        _localizer.Format("Member.Background.UnknownError", userLocale, Bot.ApplicatonOwner),
                                        logChannel, _localizer, guildLocale);
                                    continue;
                                }
                            }
                            catch (Exception ex2)
                            {
                                Log.Error($"CheckMemberStatus: {guildYoutubeMemberConfig.GuildId} - {member.UserId} \"{guildYoutubeMemberConfig.MemberCheckChannelTitle}\" 回傳會限資格訊息失敗: {ex}");
                                Log.Error(ex2.ToString());
                            }
                        }

                        if (!isMember) continue;
                        checkedMemberSet.Add($"{member.UserId}-{member.CheckYTChannelId}");
                    }

                    checkedMemberCount++;
                    totalIsMemberCount++;
                    bool isCantAddRold = true;
                    try
                    {
                        // 任何驗證成功都確保給組（AddRole idempotent）：修復「已驗證會員被踢出後重加入，
                        // 舊檢查成功卻不補組」的問題。確認訊息仍只在新檢查發（見下方 if (!isOldCheck && !isCantAddRold)），
                        // 舊檢查靜默補組不 spam；若成員已離開，UnknownMember catch 會順便從 DB 清除。
                        await _client.Rest.AddRoleAsync(guild.Id, member.UserId, role.Id).ConfigureAwait(false);
                        _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Add, YoutubeMemberRoleResult.Success);
                        isCantAddRold = false;
                    }
                    catch (Discord.Net.HttpException httpEx)
                    {
                        if (httpEx.DiscordCode.HasValue && (httpEx.DiscordCode.Value == DiscordErrorCode.MissingPermissions || httpEx.DiscordCode.Value == DiscordErrorCode.InsufficientPermissions))
                        {
                            _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Add, YoutubeMemberRoleResult.MissingPermission);
                            Log.Error(httpEx, $"無法新增用戶組至用戶: {guild.Id} / {member.UserId}");

                            await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                _localizer.Get("Member.Status.VerifiedRolePermissionFailed", guildLocale), _localizer, guildLocale);
                            await member.UserId.SendConfirmMessageAsync(_client,
                                _localizer.Format("Member.Background.VerifiedRoleFailed", userLocale,
                                    guild.Name, guildYoutubeMemberConfig.MemberCheckChannelTitle),
                                logChannel, _localizer, guildLocale);
                        }
                        else if (httpEx.DiscordCode.HasValue && (httpEx.DiscordCode.Value == DiscordErrorCode.UnknownAccount || httpEx.DiscordCode.Value == DiscordErrorCode.UnknownMember || httpEx.DiscordCode.Value == DiscordErrorCode.UnknownUser))
                        {
                            _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Add, YoutubeMemberRoleResult.UserMissing);
                            await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                _localizer.Get("Member.Status.UnknownUser", guildLocale), _localizer, guildLocale);
                            Log.Warn($"用戶已離開伺服器: {guild.Id} / {member.UserId}");
                            db.YoutubeMemberCheck.Remove(member);
                        }
                        else
                        {
                            _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Add, YoutubeMemberRoleResult.DiscordError);
                            Log.Error(httpEx, $"無法新增用戶組至用戶: {guild.Id} / {member.UserId}");

                            await logChannel.SendErrorMessageAsync(_client, member.UserId,
                                guildYoutubeMemberConfig.MemberCheckChannelTitle,
                                _localizer.Get("Member.Status.VerifiedRoleUnknownError", guildLocale), _localizer, guildLocale);
                            await member.UserId.SendConfirmMessageAsync(_client,
                                _localizer.Format("Member.Background.VerifiedRoleFailed", userLocale,
                                    guild.Name, guildYoutubeMemberConfig.MemberCheckChannelTitle),
                                logChannel, _localizer, guildLocale);
                        }
                    }

                    try
                    {
                        member.IsChecked = true;
                        member.LastCheckTime = DateTime.Now;

                        if (!isOldCheck && !isCantAddRold)
                        {
                            try
                            {
                                await logChannel.SendConfirmMessageAsync(_client, member.UserId, new EmbedBuilder()
                                    .AddField(_localizer.Get("Member.Status.Channel", guildLocale), guildYoutubeMemberConfig.MemberCheckChannelTitle)
                                    .AddField(_localizer.Get("Member.Status.State", guildLocale),
                                        _localizer.Get("Member.Status.Verified", guildLocale)));
                            }
                            catch (Exception ex)
                            {
                                Log.Warn($"無法傳送紀錄訊息: {guild.Id} / {logChannel.Id}");
                                Log.Error(ex, $"無法傳送紀錄訊息: {guild.Id} / {logChannel.Id}");
                            }

                            try
                            {
                                await member.UserId.SendConfirmMessageAsync(_client,
                                    _localizer.Format("Member.Background.Verified", userLocale,
                                        guild.Name, guildYoutubeMemberConfig.MemberCheckChannelTitle),
                                    logChannel, _localizer, guildLocale);
                            }
                            catch (Exception ex)
                            {
                                Log.Warn($"無法傳送私訊: {guild.Id} / {member.UserId}");
                                Log.Error(ex, $"無法傳送私訊: {guild.Id} / {member.UserId}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"MemberCheckUpdateDb: {guildYoutubeMemberConfig.GuildId} - {member.UserId} 資料庫更新失敗");
                    }
                }

                await logChannel.SendConfirmMessageAsync(
                    _localizer.Get(isOldCheck ? "Member.Status.OldCheckComplete" : "Member.Status.NewCheckComplete", guildLocale),
                    _localizer.Format("Member.Status.CheckSummary", guildLocale,
                        guildYoutubeMemberConfig.MemberCheckChannelTitle, totalCheckCount, checkedMemberCount));

                var saveTime = DateTime.Now;
                bool saveFailed = false;
                int retryCount = 0;
                const int maxRetryCount = 5;

                do
                {
                    try
                    {
                        await db.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        saveFailed = true;
                        retryCount++;
                        foreach (var item in ex.Entries)
                        {
                            try
                            {
                                item.Reload();
                            }
                            catch (Exception ex2)
                            {
                                Log.Error(ex2.Demystify(), $"MainDb-CheckMemberShip-SaveChanges-Reload");
                                Log.Error(item.DebugView.ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"MainDb-CheckMemberShip-SaveChanges");
                        Log.Error(db.ChangeTracker.DebugView.LongView);
                    }
                } while (saveFailed && retryCount < maxRetryCount && DateTime.Now.Subtract(saveTime) <= TimeSpan.FromMinutes(1));
            }

            if (totalCheckMemberCount > 0)
            {
                Log.Info((isOldCheck ? "舊" : "新") + $"會限檢查完畢");
                Log.Info($"總驗證: {totalCheckMemberCount} 位，成功驗證: {totalIsMemberCount} 位，驗證失敗: {totalCheckMemberCount - totalIsMemberCount} 位");
            }
        }
    }
}
