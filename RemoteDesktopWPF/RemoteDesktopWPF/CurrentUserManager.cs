using RemoteDesktopWPF.Views;

public class CurrentUserManager
{
    private static CurrentUserManager _instance;
    public static CurrentUserManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CurrentUserManager();
            }
            return _instance;
        }
    }

    public string CurrentUsername { get; set; }

    public UserConfig CurrentUserConfig { get; set; }

    private CurrentUserManager() { }
}
