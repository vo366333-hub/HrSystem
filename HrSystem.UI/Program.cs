using HrSystem.Core.Services;
using HrSystem.Storage;
using HrSystem.Storage.Seed;
using HrSystem.UI.Forms;

namespace HrSystem.UI;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "hr_data.json");
            var repository = new JsonHrRepository(dataPath);

            // јвтоматичне заповненн€ тестовими даними при першому запуску
            SampleHrDataSeeder.SeedIfEmpty(repository);

            var hrService = new HrService(repository);
            Application.Run(new MainForm(hrService));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"ѕомилка запуску застосунку:\n{ex.Message}",
                " ритична помилка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}