namespace LeafLINQWeb.Models;

public class PseudoSession
{
    private static PseudoSession _instance = null;
    public Guid Id { get;set;}
    public string BackgroundExternalAPIToken { get;set;}
    public string BackgroundLocalAPIToken { get;set;}


    // Get our single instance for duration of program execution.
    public static PseudoSession Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new PseudoSession();
                _instance.Id = Guid.NewGuid();
            }
            return _instance;
        }
    }
}
