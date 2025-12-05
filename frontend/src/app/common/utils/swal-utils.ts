/**
 * SweetAlert2 Utility Functions
 * Paylaşılabilir SweetAlert2 dialog helpers ve custom componentler
 */

/**
 * Toggle switch için HTML ve CSS oluşturur
 * @param id - Checkbox input id
 * @param label - Toggle yanındaki label metni
 * @param checked - Başlangıç durumu
 * @returns HTML string
 */
export function createToggleSwitch(id: string, label: string, checked: boolean = false): string {
  return `
    <label class="custom-switch" style="display: flex; align-items: center; justify-content: center; margin-top: 15px; cursor: pointer;">
      <input type="checkbox" id="${id}" ${checked ? 'checked' : ''} style="display: none;">
      <span class="switch-slider"></span>
      <span style="margin-left: 10px; font-weight: 500;">${label}</span>
    </label>
  `;
}

/**
 * Toggle switch için gerekli CSS
 */
export const TOGGLE_SWITCH_CSS = `
  <style>
    .custom-switch { position: relative; }
    .switch-slider {
      width: 50px;
      height: 26px;
      background-color: #ccc;
      border-radius: 13px;
      position: relative;
      transition: background-color 0.3s;
    }
    .switch-slider::before {
      content: '';
      position: absolute;
      width: 20px;
      height: 20px;
      border-radius: 50%;
      background-color: white;
      top: 3px;
      left: 3px;
      transition: transform 0.3s;
      box-shadow: 0 2px 4px rgba(0,0,0,0.2);
    }
    input[type="checkbox"]:checked + .switch-slider {
      background-color: #1976d2;
    }
    input[type="checkbox"]:checked + .switch-slider::before {
      transform: translateX(24px);
    }
  </style>
`;

/**
 * Toggle switch event handler'ını initialize eder
 * SweetAlert didOpen callback'inde kullanılır
 * @param checkboxId - Checkbox input id
 */
export function initializeToggleSwitch(checkboxId: string): void {
  const checkbox = document.getElementById(checkboxId) as HTMLInputElement;

  if (!checkbox) return;

  // Checkbox'ın parent'ı olan label içinden slider'ı bul
  const label = checkbox.parentElement;
  const slider = label?.querySelector('.switch-slider') as HTMLElement;

  if (!slider) return;

  // Slider'a click event ekle
  slider.addEventListener('click', (e) => {
    e.stopPropagation();
    checkbox.checked = !checkbox.checked;
  });

  // Label'a da click event ekle (tüm alana tıklanabilir)
  label?.addEventListener('click', (e) => {
    if (e.target === slider || (e.target as HTMLElement).classList.contains('switch-slider')) {
      return; // Slider'a tıklandıysa label event'ini ignore et
    }
    e.stopPropagation();
    checkbox.checked = !checkbox.checked;
  });
}

/**
 * Birden fazla toggle switch için event handler'ları initialize eder
 * @param checkboxIds - Checkbox input id'leri
 */
export function initializeToggleSwitches(checkboxIds: string[]): void {
  checkboxIds.forEach(id => {
    const checkbox = document.getElementById(id) as HTMLInputElement;
    const sliders = document.querySelectorAll('.switch-slider');

    if (checkbox && sliders.length > 0) {
      // Her checkbox için kendi slider'ını bul (parent üzerinden)
      const parent = checkbox.parentElement;
      const slider = parent?.querySelector('.switch-slider') as HTMLElement;

      if (slider) {
        slider.addEventListener('click', (e) => {
          e.stopPropagation();
          checkbox.checked = !checkbox.checked;
        });
      }
    }
  });
}
