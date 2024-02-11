using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace vmb
{
    public class ApplicationCommandObject
    {
        public string id { get; set; }
        public string application_id { get; set; }
        public Int16 type { get; set; }
        public ApplicationCommandData data { get; set; }
        public string guild_id { get; set; }
        public Channel channel { get; set; }
        public string channel_id { get; set; }
        public GuildMember member { get; set; }
        public User user { get; set; }
        public string token { get; set; }
        public Int16 version { get; set; }
        public string app_permissions { get; set; }
        public string locale { get; set; }
        public string guild_locale { get; set; }
    }
    public class ApplicationCommandData
    {
        public string id { get; set; }
        public string name { get; set; }
        public int type { get; set; }
        public RosolvedData resolved { get; set; }
        public ApplicationCommandOption[] options { get; set; }
        public Int16 guild_id { get; set; }
        public Int16 target_id { get; set; }
    }
    public class RosolvedData
    {
        public Dictionary<Int16, User> users { get; set; }
        public Dictionary<Int16, GuildMember> members { get; set; }
        public Dictionary<Int16, Channel> channels { get; set; }
        public Dictionary<Int16, Attachment> attachments { get; set; }
    }
    public class ApplicationCommandOption
    {
        public Int32 id { get; set; }
        public Int16 type { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public bool required { get; set; }
        public ApplicationCommandOption[] options { get; set; }
    }
    public class Channel
    {

    }
    public class GuildMember
    {
        public User user { get; set; }
        public string nick { get; set; }
        public string avatar { get; set; }
    }
    public class User
    {
        public string id { get; set; }
        public string username { get; set; }
        public string discriminator { get; set; }
        public string global_name { get; set; }
    }
    //work in progress
    public class InteractionResponse
    {
        public Int16 type { get; set; }
        public InteractionCallbackData data { get; set; }
    }
    public class InteractionCallbackData
    {
        public string content { get; set; }
    }
}
