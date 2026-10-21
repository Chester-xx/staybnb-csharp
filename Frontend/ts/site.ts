// Dropdown toggles: <button data-toggle="menu-id"> opens <div id="menu-id" data-menu class="hidden">.
function initMenus(): void {
  document.querySelectorAll<HTMLElement>("[data-toggle]").forEach((button) => {
    button.addEventListener("click", (event) => {
      event.stopPropagation();
      const menu = document.getElementById(button.dataset.toggle ?? "");
      if (!menu) return;
      const wasHidden = menu.classList.contains("hidden");
      closeMenus();
      if (wasHidden) menu.classList.remove("hidden");
    });
  });

  document.addEventListener("click", closeMenus);
}

function closeMenus(): void {
  document.querySelectorAll<HTMLElement>("[data-menu]").forEach((menu) => menu.classList.add("hidden"));
}

// Selection mode for bulk actions (e.g. removing properties on My Properties).
// Checkboxes and buttons point at the empty #delete-form via their form attribute.
function initSelectionMode(): void {
  const form = document.getElementById("delete-form") as HTMLFormElement | null;
  if (!form) return;

  const toggle = document.querySelector<HTMLElement>("[data-select-toggle]");
  const cancel = document.querySelector<HTMLElement>("[data-select-cancel]");
  const actions = document.querySelector<HTMLElement>("[data-select-actions]");
  const boxes = Array.from(document.querySelectorAll<HTMLInputElement>("[data-select-box]"));

  const setMode = (on: boolean): void => {
    boxes.forEach((box) => {
      box.classList.toggle("hidden", !on);
      if (!on) box.checked = false;
    });
    actions?.classList.toggle("hidden", !on);
    toggle?.classList.toggle("hidden", on);
  };

  toggle?.addEventListener("click", () => setMode(true));
  cancel?.addEventListener("click", () => setMode(false));

  form.addEventListener("submit", (event) => {
    const count = boxes.filter((box) => box.checked).length;
    if (count === 0) {
      event.preventDefault();
      alert("Select at least one property to remove.");
      return;
    }
    const noun = count === 1 ? "property" : "properties";
    if (!confirm(`Permanently remove ${count} selected ${noun}? This cannot be undone.`)) {
      event.preventDefault();
    }
  });
}

// Add/remove rule inputs on the Check-in Setup page.
function initRuleList(): void {
  const list = document.querySelector<HTMLElement>("[data-rule-list]");
  const addButton = document.querySelector<HTMLElement>("[data-rule-add]");
  const template = document.querySelector<HTMLTemplateElement>("[data-rule-template]");
  if (!list || !addButton || !template) return;

  addButton.addEventListener("click", () => {
    const row = template.content.firstElementChild?.cloneNode(true) as HTMLElement | undefined;
    if (!row) return;
    list.appendChild(row);
    row.querySelector<HTMLInputElement>("input")?.focus();
  });

  list.addEventListener("click", (event) => {
    const removeButton = (event.target as HTMLElement).closest("[data-rule-remove]");
    removeButton?.closest("[data-rule-row]")?.remove();
  });
}

// Live price breakdown on the property page. The server recalculates the total when booking.
function initBookingCalculator(): void {
  const card = document.querySelector<HTMLElement>("[data-booking-card]");
  const checkIn = document.getElementById("CheckInDate") as HTMLInputElement | null;
  const checkOut = document.getElementById("CheckOutDate") as HTMLInputElement | null;
  if (!card || !checkIn || !checkOut) return;

  const price = parseFloat(card.dataset.price ?? "0");
  const cleaningPercent = parseFloat(card.dataset.cleaningPercent ?? "0");
  const servicePercent = parseFloat(card.dataset.servicePercent ?? "0");
  const error = card.querySelector<HTMLElement>("[data-booking-error]");
  const submit = card.querySelector<HTMLButtonElement>("[data-booking-submit]");
  const booked = JSON.parse(card.dataset.booked ?? "[]") as { from: string; to: string }[];

  const showError = (message: string | null): void => {
    if (error) {
      error.textContent = message ?? "";
      error.classList.toggle("hidden", message === null);
    }
    if (submit) submit.disabled = message !== null;
  };

  const setLine = (name: string, text: string): void => {
    const element = card.querySelector<HTMLElement>(`[data-line="${name}"]`);
    if (element) element.textContent = text;
  };
  const round = (value: number): number => Math.round((value + Number.EPSILON) * 100) / 100;
  const money = (value: number): string =>
    "R " + value.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const toDateValue = (date: Date): string => {
    const pad = (n: number): string => String(n).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
  };

  const update = (): void => {
    if (checkIn.value) {
      const nextDay = new Date(`${checkIn.value}T00:00:00`);
      nextDay.setDate(nextDay.getDate() + 1);
      const minValue = toDateValue(nextDay);
      checkOut.min = minValue;
      if (!checkOut.value || checkOut.value < minValue) checkOut.value = minValue;
    }

    const nights = Math.round(
      (new Date(`${checkOut.value}T00:00:00`).getTime() - new Date(`${checkIn.value}T00:00:00`).getTime()) / 86_400_000
    );

    if (!Number.isFinite(nights) || nights < 1) {
      showError("Check-out must be after check-in.");
      ["subtotal-label", "subtotal", "cleaning", "service", "total"].forEach((name) => setLine(name, ""));
      return;
    }

    // The check-out day of one stay is free for the next guest to check in.
    const overlaps = booked.some((range) => checkIn.value < range.to && checkOut.value > range.from);
    showError(overlaps ? "Those dates are already booked. Please choose different dates." : null);

    const subtotal = round(price * nights);
    const cleaning = round((subtotal * cleaningPercent) / 100);
    const service = round((subtotal * servicePercent) / 100);

    setLine("subtotal-label", `${money(price)} x ${nights} night${nights === 1 ? "" : "s"}`);
    setLine("subtotal", money(subtotal));
    setLine("cleaning", money(cleaning));
    setLine("service", money(service));
    setLine("total", money(subtotal + cleaning + service));
  };

  checkIn.addEventListener("change", update);
  checkOut.addEventListener("change", update);
  update();
}

document.addEventListener("DOMContentLoaded", () => {
  initMenus();
  initSelectionMode();
  initRuleList();
  initBookingCalculator();
});
