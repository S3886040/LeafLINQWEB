namespace LeafLINQWeb.Models
{
    public class SettingsModel
    {
        public string SupportInformation { get; set; } = string.Empty;
        public char TemperatureUnit { get; set; } = 'C';
        public bool EnableNotifications { get; set; } = true;
    }
}
