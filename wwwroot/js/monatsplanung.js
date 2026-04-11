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

    const antiforgery = () => ({
        "Content-Type": "application/json",
        "RequestVerificationToken": tokenInput ? tokenInput.value : ""
    });

    const updateEmployeeRest = (employeeRest) => {
        if (!employeeRest) return;

        Object.entries(employeeRest).forEach(([employeeId, rest]) => {
            const target = document.getElementById(`employee-rest-${employeeId}`);
            if (target) {
                target.textContent = `Rest: ${rest} h`;
            }
        });
    };

    const updateSlotDom = (slotData) => {
        if (!slotData) return;

        const zone = document.querySelector(
            `.slot-dropzone[data-standort-id="${slotData.standortId}"][data-datum="${slotData.datum}"][data-slot="${slotData.slot}"]`
        );

        if (!zone) return;

        const existing = zone.querySelector(".slot-belegung");
        if (existing) {
            existing.remove();
        }

        if (!slotData.mitarbeiterName) {
            return;
        }

        const belegung = document.createElement("div");
        belegung.className = "slot-belegung compact-slot-belegung";
        belegung.style.background = slotData.farbe ?? "";
        belegung.draggable = true;
        belegung.dataset.mitarbeiterId = slotData.mitarbeiterId;
        belegung.dataset.mitarbeiterName = slotData.mitarbeiterName;
        belegung.dataset.color = slotData.farbe ?? "";
        belegung.dataset.sourceStandortId = slotData.standortId;
        belegung.dataset.sourceDatum = slotData.datum;
        belegung.dataset.sourceSlot = slotData.slot;

        const name = document.createElement("span");
        name.className = "slot-belegung-name";
        name.textContent = slotData.mitarbeiterName;

        const removeButton = document.createElement("button");
        removeButton.type = "button";
        removeButton.className = "remove-slot icon-button";
        removeButton.dataset.standortId = slotData.standortId;
        removeButton.dataset.datum = slotData.datum;
        removeButton.dataset.slot = slotData.slot;
        removeButton.title = "Belegung entfernen";
        removeButton.textContent = "×";

        belegung.appendChild(name);
        belegung.appendChild(removeButton);
        zone.appendChild(belegung);

        bindAssignedDrag(belegung);
        bindRemoveButton(removeButton);
    };

    const saveDrop = async (payload) => {
        const response = await fetch("/Monatsplanung/AssignSlot", {
            method: "POST",
            headers: antiforgery(),
            body: JSON.stringify(payload)
        });

        const data = await response.json();
        return { response, data };
    };

    const removeSlot = async (payload) => {
        const response = await fetch("/Monatsplanung/RemoveSlot", {
            method: "POST",
            headers: antiforgery(),
            body: JSON.stringify(payload)
        });

        const data = await response.json();
        return { response, data };
    };

    const handleAssign = async (request) => {
        let result = await saveDrop(request);

        if (!result.response.ok && result.data?.requiresConfirmation) {
            const confirmed = window.confirm(
                `${result.data.message}\n\nTrotzdem einplanen?`
            );

            if (!confirmed) {
                return null;
            }

            request.forceMaxHoursOverride = true;
            result = await saveDrop(request);
        }

        if (!result.response.ok || !result.data.success) {
            showStatus(result.data?.message || "Speichern fehlgeschlagen.", true);
            return null;
        }

        updateSlotDom(result.data.slot);
        updateEmployeeRest(result.data.employeeRest);
        return result.data;
    };

    const bindAssignedDrag = (element) => {
        element.addEventListener("dragstart", (event) => {
            const payload = {
                type: "assigned",
                mitarbeiterId: element.dataset.mitarbeiterId,
                mitarbeiterName: element.dataset.mitarbeiterName,
                color: element.dataset.color,
                sourceStandortId: element.dataset.sourceStandortId,
                sourceDatum: element.dataset.sourceDatum,
                sourceSlot: element.dataset.sourceSlot
            };

            event.dataTransfer.setData("application/json", JSON.stringify(payload));
            event.dataTransfer.effectAllowed = "move";
        });
    };

    if (standort) standort.addEventListener("change", submitFilter);
    if (jahr) jahr.addEventListener("change", submitFilter);
    if (monat) monat.addEventListener("change", submitFilter);

    document.querySelectorAll(".mitarbeiter-card").forEach((item) => {
        item.addEventListener("dragstart", (event) => {
            const payload = {
                type: "sidebar",
                mitarbeiterId: item.dataset.mitarbeiterId,
                mitarbeiterName: item.dataset.mitarbeiterName,
                color: item.dataset.color
            };

            event.dataTransfer.setData("application/json", JSON.stringify(payload));
            event.dataTransfer.effectAllowed = "move";
        });
    });

    document.querySelectorAll(".slot-belegung").forEach(bindAssignedDrag);

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

            const targetStandortId = Number(zone.dataset.standortId);
            const targetDatum = zone.dataset.datum;
            const targetSlot = Number(zone.dataset.slot);

            if (dragged.type === "assigned") {
                const sameTarget =
                    Number(dragged.sourceStandortId) === targetStandortId &&
                    dragged.sourceDatum === targetDatum &&
                    Number(dragged.sourceSlot) === targetSlot;

                if (sameTarget) {
                    return;
                }

                const assignRequest = {
                    standortId: targetStandortId,
                    mitarbeiterId: Number(dragged.mitarbeiterId),
                    datum: targetDatum,
                    slot: targetSlot,
                    forceMaxHoursOverride: false
                };

                const assignData = await handleAssign(assignRequest);
                if (!assignData) {
                    return;
                }

                const removeResult = await removeSlot({
                    standortId: Number(dragged.sourceStandortId),
                    datum: dragged.sourceDatum,
                    slot: Number(dragged.sourceSlot)
                });

                if (!removeResult.response.ok || !removeResult.data.success) {
                    showStatus(removeResult.data?.message || "Verschieben teilweise fehlgeschlagen.", true);
                    return;
                }

                updateSlotDom(removeResult.data.slot);
                updateEmployeeRest(removeResult.data.employeeRest);
                showStatus("Belegung verschoben.");
                return;
            }

            const request = {
                standortId: targetStandortId,
                mitarbeiterId: Number(dragged.mitarbeiterId),
                datum: targetDatum,
                slot: targetSlot,
                forceMaxHoursOverride: false
            };

            const result = await handleAssign(request);
            if (result) {
                showStatus("Belegung gespeichert.");
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
                    headers: antiforgery(),
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
                    headers: antiforgery(),
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

    const bindRemoveButton = (button) => {
        button.addEventListener("click", async (event) => {
            event.stopPropagation();

            const request = {
                standortId: Number(button.dataset.standortId),
                datum: button.dataset.datum,
                slot: Number(button.dataset.slot)
            };

            try {
                const response = await fetch("/Monatsplanung/RemoveSlot", {
                    method: "POST",
                    headers: antiforgery(),
                    body: JSON.stringify(request)
                });

                const data = await response.json();

                if (!response.ok || !data.success) {
                    showStatus(data.message || "Belegung konnte nicht entfernt werden.", true);
                    return;
                }

                updateSlotDom(data.slot);
                updateEmployeeRest(data.employeeRest);
                showStatus("Belegung entfernt.");
            } catch {
                showStatus("Belegung konnte nicht entfernt werden.", true);
            }
        });
    };

    document.querySelectorAll(".remove-slot").forEach(bindRemoveButton);
});