using MonkeyFinder.Services;
using System.Linq.Expressions;

namespace MonkeyFinder.ViewModel;

public partial class MonkeysViewModel : BaseViewModel
{
    MonkeyService monkeyService;
    public ObservableCollection<Monkey> Monkeys { get; } = new ObservableCollection<Monkey>();

    IConnectivity connectivity;
    IGeolocation geolocation;

    public MonkeysViewModel(MonkeyService monkeyService, IConnectivity connectivity, IGeolocation geolocation)
    {
        Title = "Monkey Finder";
        this.monkeyService = monkeyService;
        this.connectivity = connectivity;
        this.geolocation = geolocation;
    }

    [ObservableProperty]
    bool isRefreshing;

    [RelayCommand]
    async Task GetClosestMonkeyAsync()
    {
        

        if(IsBusy || Monkeys.Count == 0) return;

        try
        {
            var location = await geolocation.GetLastKnownLocationAsync();
            if(location == null)
            {
                location = await geolocation.GetLocationAsync(
                    new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(30),
                    });
            }

            if (location == null) return;

            var first = Monkeys.OrderBy(m =>
                location.CalculateDistance(m.Latitude, m.Latitude, DistanceUnits.Kilometers))
                .FirstOrDefault();
            if (first == null) return;

            await Shell.Current.DisplayAlert("Closest Monkey",
                $"{first.Name} in {first.Location}", "OK");
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex);
            await Shell.Current.DisplayAlert("Error!", "Unable to get closest monkey", "OK");

        }
    }

    [RelayCommand]
    async Task GoToDetailsAsync(Monkey monkey)
    {
        if(monkey == null) return;

        await Shell.Current.GoToAsync($"{nameof(DetailsPage)}", true, 
            new Dictionary<string, object>
            {
                {"Monkey", monkey }
            });
    }


    [RelayCommand]
    async Task GetMonkeysAsync()
    {
        if(IsBusy) return;

        try
        {
            if(connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert("Internet issue",
                    "Check your internet and try again!", "OK");
                return;
            }
            IsBusy = true;
            var monkeys = await monkeyService.GetMonkeys();

            if(Monkeys.Count != 0) Monkeys.Clear();

            foreach(var monkey in monkeys) Monkeys.Add(monkey); 
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex);
            await Shell.Current.DisplayAlert("Error!", "Unable to get Monkeys", "OK");
        }
        finally 
        {
            IsBusy = false;
            isRefreshing = false;
        }
    }
}
