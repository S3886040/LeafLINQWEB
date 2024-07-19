namespace LeafLINQWeb.Models
{
    public class ReportsModel
    {
        public PlantAPISensorReadingAverage DailyReports { get; set; }
        public List<PlantAPISensorReadingDailyAverage> MonthlyReports { get; set; }
    }
}
