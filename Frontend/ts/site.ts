
// Define init menus function
function initMenus(): void {
  // Attach a click handler to every menu toggle button
  document.querySelectorAll<HTMLElement>("[data-toggle]").forEach((button) => {
    button.addEventListener("click", (event) => {
      // Stop the click from reaching the document handler that closes menus
      event.stopPropagation();

      // Get the menu targeted by the button, if none exists, return
      const menu = document.getElementById(button.dataset.toggle ?? "");
      if (!menu) return;

      // Close all menus, then reopen the target menu only if it was hidden before
      const wasHidden = menu.classList.contains("hidden");
      closeMenus();
      if (wasHidden) menu.classList.remove("hidden");
    });
  });

  // Close all menus when clicking anywhere else on the page
  document.addEventListener("click", closeMenus);
}

// Define close menus function
function closeMenus(): void {
  // Hide every menu on the page
  document.querySelectorAll<HTMLElement>("[data-menu]").forEach((menu) => menu.classList.add("hidden"));
}

// Define init selection mode function (bulk property removal)
function initSelectionMode(): void {
  // Get the delete form, if it is not on this page, return
  const form = document.getElementById("delete-form") as HTMLFormElement | null;
  if (!form) return;

  // Get the toggle, cancel, actions and checkbox elements
  const toggle = document.querySelector<HTMLElement>("[data-select-toggle]");
  const cancel = document.querySelector<HTMLElement>("[data-select-cancel]");
  const actions = document.querySelector<HTMLElement>("[data-select-actions]");
  const boxes = Array.from(document.querySelectorAll<HTMLInputElement>("[data-select-box]"));

  // Show or hide the checkboxes and actions, clearing any ticked boxes when turning selection off
  const setMode = (on: boolean): void => {
    boxes.forEach((box) => {
      box.classList.toggle("hidden", !on);
      if (!on) box.checked = false;
    });
    actions?.classList.toggle("hidden", !on);
    toggle?.classList.toggle("hidden", on);
  };

  // Enter selection mode on toggle, leave it on cancel
  toggle?.addEventListener("click", () => setMode(true));
  cancel?.addEventListener("click", () => setMode(false));

  form.addEventListener("submit", (event) => {
    // Count the selected properties, if none are selected, block the submit with an alert
    const count = boxes.filter((box) => box.checked).length;
    if (count === 0) {
      event.preventDefault();
      alert("Select at least one property to remove.");
      return;
    }

    // Ask for confirmation, if the user declines, block the submit
    const noun = count === 1 ? "property" : "properties";
    if (!confirm(`Permanently remove ${count} selected ${noun}? This cannot be undone.`)) {
      event.preventDefault();
    }
  });
}

// Define init rule list function (add / remove rule rows)
function initRuleList(): void {
  // Get the list, add button and row template, if any are missing, return
  const list = document.querySelector<HTMLElement>("[data-rule-list]");
  const addButton = document.querySelector<HTMLElement>("[data-rule-add]");
  const template = document.querySelector<HTMLTemplateElement>("[data-rule-template]");
  if (!list || !addButton || !template) return;

  addButton.addEventListener("click", () => {
    // Clone the template row, if it is empty, return
    const row = template.content.firstElementChild?.cloneNode(true) as HTMLElement | undefined;
    if (!row) return;

    // Append the new row and focus its input
    list.appendChild(row);
    row.querySelector<HTMLInputElement>("input")?.focus();
  });

  // Remove a row when its remove button is clicked (event delegation, so added rows work too)
  list.addEventListener("click", (event) => {
    const removeButton = (event.target as HTMLElement).closest("[data-rule-remove]");
    removeButton?.closest("[data-rule-row]")?.remove();
  });
}

// Define init booking calculator function
function initBookingCalculator(): void {
  // Get the booking card and date inputs, if any are missing, return
  const card = document.querySelector<HTMLElement>("[data-booking-card]");
  const checkIn = document.getElementById("CheckInDate") as HTMLInputElement | null;
  const checkOut = document.getElementById("CheckOutDate") as HTMLInputElement | null;
  if (!card || !checkIn || !checkOut) return;

  // Read the pricing values, error / submit elements and booked date ranges from the card
  const price = parseFloat(card.dataset.price ?? "0");
  const cleaningPercent = parseFloat(card.dataset.cleaningPercent ?? "0");
  const servicePercent = parseFloat(card.dataset.servicePercent ?? "0");
  const error = card.querySelector<HTMLElement>("[data-booking-error]");
  const submit = card.querySelector<HTMLButtonElement>("[data-booking-submit]");
  const booked = JSON.parse(card.dataset.booked ?? "[]") as { from: string; to: string }[];

  // Show or clear the error message, and disable submit while an error is shown
  const showError = (message: string | null): void => {
    if (error) {
      error.textContent = message ?? "";
      error.classList.toggle("hidden", message === null);
    }
    if (submit) submit.disabled = message !== null;
  };

  // Set the text of a price line by name
  const setLine = (name: string, text: string): void => {
    const element = card.querySelector<HTMLElement>(`[data-line="${name}"]`);
    if (element) element.textContent = text;
  };

  // Round to 2 decimal places
  const round = (value: number): number => Math.round((value + Number.EPSILON) * 100) / 100;

  // Format a number as rands
  const money = (value: number): string =>
    "R " + value.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

  // Format a date as yyyy-MM-dd for date inputs
  const toDateValue = (date: Date): string => {
    const pad = (n: number): string => String(n).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
  };

  // Define update method
  const update = (): void => {
    // Keep check-out at least one day after check-in
    if (checkIn.value) {
      const nextDay = new Date(`${checkIn.value}T00:00:00`);
      nextDay.setDate(nextDay.getDate() + 1);
      const minValue = toDateValue(nextDay);
      checkOut.min = minValue;
      if (!checkOut.value || checkOut.value < minValue) checkOut.value = minValue;
    }

    // Calculate the number of nights (86,400,000 ms per day)
    const nights = Math.round(
      (new Date(`${checkOut.value}T00:00:00`).getTime() - new Date(`${checkIn.value}T00:00:00`).getTime()) / 86_400_000
    );

    // If the dates are invalid, show an error, clear the price lines and return
    if (!Number.isFinite(nights) || nights < 1) {
      showError("Check-out must be after check-in.");
      ["subtotal-label", "subtotal", "cleaning", "service", "total"].forEach((name) => setLine(name, ""));
      return;
    }

    // Check if the selected dates overlap any booked range, if so, show an error
    const overlaps = booked.some((range) => checkIn.value < range.to && checkOut.value > range.from);
    showError(overlaps ? "Those dates are already booked. Please choose different dates." : null);

    // Calculate the subtotal, cleaning fee and service fee
    const subtotal = round(price * nights);
    const cleaning = round((subtotal * cleaningPercent) / 100);
    const service = round((subtotal * servicePercent) / 100);

    // Update the price lines in the card
    setLine("subtotal-label", `${money(price)} x ${nights} night${nights === 1 ? "" : "s"}`);
    setLine("subtotal", money(subtotal));
    setLine("cleaning", money(cleaning));
    setLine("service", money(service));
    setLine("total", money(subtotal + cleaning + service));
  };

  // Recalculate whenever either date changes, and once on load
  checkIn.addEventListener("change", update);
  checkOut.addEventListener("change", update);
  update();
}

// Run all initialisers once the page has loaded
document.addEventListener("DOMContentLoaded", () => {
  initMenus();
  initSelectionMode();
  initRuleList();
  initBookingCalculator();
});
