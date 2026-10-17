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

document.addEventListener("DOMContentLoaded", initMenus);
