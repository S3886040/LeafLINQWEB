namespace LeafLINQWeb.Models;

public class EmailModel
{

    public EmailModel() 
    {
        To = new List<string>();
    }
    public string From { get; set; }
    public List<string> To { get; set; }
    public string Body { get; set; }
    public string Subject { get; set; }
}
