using Discord.Interactions;
using DiscordStreamNotifyBot.Interaction.Attribute;
using static DiscordStreamNotifyBot.Interaction.OwnerOnly.Service.SendMsgToAllGuildService;

namespace DiscordStreamNotifyBot.Interaction.OwnerOnly
{
    [DontAutoRegister]
    [RequireGuild(506083124015398932)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class SendMsgToAllGuild : TopLevelModule<Service.SendMsgToAllGuildService>
    {
        [SlashCommand("send-message", "傳送訊息到所有伺服器")]
        [RequireOwner]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        public async Task SendMessageToAllGuildAsync()
        {
            var radioGroupBuilder = new RadioGroupBuilder()
                .WithCustomId("notice_type")
                .WithRequired(true);

            foreach (var item in Enum.GetValues(typeof(NoticeType)))
            {
                radioGroupBuilder.AddOption(_service.GetNoticeTypeDisplayName((NoticeType)item), item.ToString());
            }

            var mb = new ModalBuilder()
                .WithTitle("傳送全球訊息")
                .WithCustomId("send_message")
                .AddRadioGroup("發送類型", radioGroupBuilder)
                .AddTextInput("訊息", "message", TextInputStyle.Paragraph, "內容...", required: true)
                .AddFileUpload("圖片", "image_attachment", isRequired: false,maxValues: 1);

            await Context.Interaction.RespondWithModalAsync(mb.Build());
        }
    }
}