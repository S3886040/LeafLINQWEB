using DotLiquid;

namespace LeafLINQWeb.Models
{
    public class EmailAlertMsg
    {
        public string First_Name { get; set; } = null!;
        public string Device { get; set; } = null!;
        public string Plant {  get; set; } = null!;
        public string Sensor_Data { get; set; }
    }

    public class EmailAlertMsgDrop : Drop
    {
        private EmailAlertMsg _emailAlertMsg;

        public EmailAlertMsgDrop(EmailAlertMsg emailAlertMsg)
        {
            _emailAlertMsg = emailAlertMsg;
        }

        public string First_Name => _emailAlertMsg.First_Name;
        public string Device => _emailAlertMsg.Device;
        public string Plant => _emailAlertMsg.Plant;
        public string Sensor_Data => _emailAlertMsg.Sensor_Data;

    }
}
