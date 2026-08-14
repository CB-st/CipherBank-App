// Legacy example: a code-behind event handler blocks the UI thread and owns
// validation, HTTP, EF, and UI state together.
public partial class MeasurementWindow : Window
{
    private readonly AppDbContext database;
    private readonly HttpClient client;

    public MeasurementWindow(AppDbContext database, HttpClient client)
    {
        InitializeComponent();
        this.database = database;
        this.client = client;
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        ImportButton.IsEnabled = false;
        StatusText.Text = string.Empty;
        try
        {
            var species = SpeciesTextBox.Text;
            var value = double.Parse(ValueTextBox.Text);
            var known = client.GetFromJsonAsync<string[]>($"species?name={species}").Result;
            if (known is null || known.Length == 0 || value < 0)
            {
                StatusText.Text = "Rejected";
                return;
            }

            database.Measurements.Add(new MeasurementEntity
            {
                Id = Guid.NewGuid(),
                Species = species,
                Value = value,
                CapturedAt = DateTime.UtcNow
            });
            database.SaveChanges();
            StatusText.Text = "Imported";
        }
        finally
        {
            ImportButton.IsEnabled = true;
        }
    }
}
