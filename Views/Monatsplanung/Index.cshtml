@model MonatsplanViewModel
@{ ViewData["Title"] = "Monatsplanung"; }
<h1>Monatsplanung</h1>
<form method="get" class="row g-3 mb-4"><div class="col-md-3"><label class="form-label">Jahr</label><input type="number" name="jahr" value="@Model.Jahr" class="form-control" /></div><div class="col-md-3"><label class="form-label">Monat</label><input type="number" name="monat" value="@Model.Monat" min="1" max="12" class="form-control" /></div><div class="col-md-3 align-self-end"><button type="submit" class="btn btn-primary">Anzeigen</button></div></form>
<table class="table table-bordered table-sm align-middle"><thead><tr><th>Mitarbeiter</th><th>Standort</th><th>Max. h</th><th>Geplant</th><th>Status</th><th>Schichten</th></tr></thead><tbody>
@foreach (var row in Model.MitarbeiterRows) {
<tr class="@(row.Ueberschritten ? "table-danger" : string.Empty)"><td>@row.MitarbeiterName</td><td>@row.StandortName</td><td>@row.MaxStunden</td><td>@row.GeplanteStunden</td><td>@(row.Ueberschritten ? "Überschritten" : "OK")</td><td>@if (row.Schichten.Any()) { <ul class="mb-0">@foreach (var s in row.Schichten) { <li>@s.Datum.ToString("dd.MM") - @s.Beginn - @s.Ende (@s.Stunden h)</li> }</ul> } else { <span>-</span> }</td></tr>
}
</tbody></table>
