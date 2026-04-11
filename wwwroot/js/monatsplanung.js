document.addEventListener("DOMContentLoaded", () => {
    const filterForm = document.getElementById("filterForm");
    const standort = document.getElementById("standortId");
    const jahr = document.getElementById("jahr");
    const monat = document.getElementById("monat");
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const statusBox = document.getElementById("planungStatus");

    const showStatus = (message, isError = false) => {
        if (!statusBox) return;

        statusBox.hidden = false;
        statusBox.textContent = message;
        statusBox.classList.toggle("error", isError);
        statusBox.classList.toggle("success", !isError);

        window.setTimeout(() => {
            statusBox.hidden = true;
        }, 3000);
    };

    const submitFilter = () => {
        if (filterForm) {
            filterForm.submit();
        }
    };

    if (standort) standort.addEventListener("change", submitFilter);
    if (jahr) jahr.addEventListener("change", submitFilter);
    if (monat) monat.addEventListener("change", submitFilter);

    document.querySelectorAll(".mitarbeiter-card").forEach((item) => {
        item.addEventListener("dragstart", (event) => {
            const payload = {
                mitarbeiterId: item.dataset.mitarbeiterId,
                mitarbeiterName: item.dataset.mitarbeiterName,
                color: item.dataset.color
            };

            event.dataTransfer.setData("application/json", JSON.stringify(payload));
            event.dataTransfer.effectAllowed = "move";
        });
    });

    document.querySelectorAll(".slot-dropzone").forEach((zone) => {
        zone.addEventListener("dragover", (event) => {
            event.preventDefault();
            zone.classList.add("dragover");
        });

        zone.addEventListener("dragleave", () => {
            zone.classList.remove("dragover");
        });

        zone.addEventListener("drop", async (event) => {
            event.preventDefault();
            zone.classList.remove("dragover");

            const raw = event.dataTransfer.getData("application/json");
            if (!raw) return;

            const dragged = JSON.parse(raw);

            const request = {
                standortId: Number(zone.dataset.standortId),
                mitarbeiterId: Number(dragged.mitarbeiterId),
                datum: zone.dataset.datum,
                slot: Number(zone.dataset.slot),
                forceMaxHoursOverride: false
            };

            const saveDrop = async (payload) => {
                const response = await fetch("/Monatsplanung/AssignSlot", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": tokenInput ? tokenInput.value : ""
                    },
                    body: JSON.stringify(payload)
                });

                const data = await response.json();
                return { response, data };
            };

            try {
                let result = await saveDrop(request);

                if (!result.response.ok && result.data?.requiresConfirmation) {
                    const confirmed = window.confirm(
                        `${result.data.message}\n\nTrotzdem einplanen?`
                    );

                    if (!confirmed) {
                        return;
                    }

                    request.forceMaxHoursOverride = true;
                    result = await saveDrop(request);
                }

                if (!result.response.ok || !result.data.success) {
                    showStatus(result.data?.message || "Speichern fehlgeschlagen.", true);
                    return;
                }

                window.location.reload();
            } catch {
                showStatus("Speichern fehlgeschlagen.", true);
            }
        });
    });

    document.querySelectorAll(".day-edit-toggle").forEach((button) => {
        button.addEventListener("click", () => {
            const targetId = button.dataset.target;
            const panel = document.getElementById(targetId);
            if (!panel) return;
            panel.hidden = !panel.hidden;
        });
    });

    document.querySelectorAll(".save-standard-slot-time").forEach((button) => {
        button.addEventListener("click", async () => {
            const slot = button.dataset.slot;
            const beginn = document.querySelector(`.standard-slot-beginn[data-slot="${slot}"]`)?.value;
            const ende = document.querySelector(`.standard-slot-ende[data-slot="${slot}"]`)?.value;

            const request = {
                standortId: Number(button.dataset.standortId),
                datum: null,
                slot: Number(slot),
                beginn,
                ende
            };

            try {
                const response = await fetch("/Monatsplanung/SaveSlotTime", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": tokenInput ? tokenInput.value : ""
                    },
                    body: JSON.stringify(request)
                });

                const data = await response.json();

                if (!response.ok || !data.success) {
                    showStatus(data.message || "Standardzeit konnte nicht gespeichert werden.", true);
                    return;
                }

                window.location.reload();
            } catch {
                showStatus("Standardzeit konnte nicht gespeichert werden.", true);
            }
        });
    });

    document.querySelectorAll(".save-day-slot-time").forEach((button) => {
        button.addEventListener("click", async () => {
            const row = button.closest(".day-edit-slot-row");
            if (!row) return;

            const beginn = row.querySelector(".day-slot-beginn")?.value;
            const ende = row.querySelector(".day-slot-ende")?.value;

            const request = {
                standortId: Number(button.dataset.standortId),
                datum: button.dataset.datum,
                slot: Number(button.dataset.slot),
                beginn,
                ende
            };

            try {
                const response = await fetch("/Monatsplanung/SaveSlotTime", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": tokenInput ? tokenInput.value : ""
                    },
                    body: JSON.stringify(request)
                });

                const data = await response.json();

                if (!response.ok || !data.success) {
                    showStatus(data.message || "Tageszeit konnte nicht gespeichert werden.", true);
                    return;
                }

                window.location.reload();
            } catch {
                showStatus("Tageszeit konnte nicht gespeichert werden.", true);
            }
        });
    });
});