@model Schicht
@{ ViewData["Title"] = "Schicht löschen"; }
<h1>Schicht löschen</h1>
<p>Möchtest du diese Schicht wirklich löschen?</p>
<ul><li><strong>Datum:</strong> @Model.Datum.ToString("dd.MM.yyyy")</li><li><strong>Mitarbeiter:</strong> @Model.Mitarbeiter?.VollerName</li><li><strong>Standort:</strong> @Model.Standort?.Name</li><li><strong>Zeitraum:</strong> @Model.Beginn - @Model.Ende</li></ul>
<form asp-action="Delete" method="post"><input asp-for="Id" type="hidden" /><button type="submit" class="btn btn-danger">Löschen</button> <a asp-action="Index" class="btn btn-secondary">Abbrechen</a></form>
