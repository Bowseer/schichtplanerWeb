using Schichtplaner.Models.ViewModels;

namespace Schichtplaner.Services;

public interface IMonatsplanPdfService
{
    byte[] Create(MonatsplanViewModel model, string standortName);
}
