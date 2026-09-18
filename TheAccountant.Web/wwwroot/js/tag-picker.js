(() => {
    "use strict";

    const normalizeWhitespace = value => value.trim().replace(/\s+/g, " ");
    const normalize = value => normalizeWhitespace(value).toUpperCase();

    function initialize(picker) {
        const valueInput = picker.querySelector("[data-tag-value]");
        const controls = picker.querySelector("[data-tag-controls]");
        const selectedList = picker.querySelector("[data-tag-selected]");
        const menu = picker.querySelector("[data-tag-menu]");
        const search = picker.querySelector("[data-tag-search]");
        const options = picker.querySelector("[data-tag-options]");
        const empty = picker.querySelector("[data-tag-empty]");
        const newTag = picker.querySelector("[data-tag-new]");
        const error = picker.querySelector("[data-tag-error]");
        const status = picker.querySelector("[data-tag-status]");
        const count = picker.querySelector("[data-tag-count]");
        const catalog = new Map();

        let selected = [];

        for (const option of picker.querySelector("[data-tag-catalog]").options) {
            const name = normalizeWhitespace(option.value);

            if (name && !catalog.has(normalize(name))) {
                catalog.set(normalize(name), name);
            }
        }

        const savedCatalog = new Map(catalog);

        function showError(message) {
            error.textContent = message;
            error.hidden = !message;
        }

        function renderOptions() {
            const term = normalize(search.value);

            const names = [...catalog.values()]
                .filter(name => normalize(name).includes(term))
                .sort((left, right) => left.localeCompare(right));

            options.replaceChildren();

            for (const name of names) {
                const label = document.createElement("label");
                label.className = "tag-picker-option";

                const checkbox = document.createElement("input");
                checkbox.type = "checkbox";
                checkbox.value = name;

                checkbox.checked = selected.some(
                    tag => normalize(tag) === normalize(name)
                );

                checkbox.disabled =
                    !checkbox.checked && selected.length >= 10;

                const text = document.createElement("span");
                text.textContent = name;

                label.append(checkbox, text);
                options.append(label);

                checkbox.addEventListener("change", () => {
                    if (checkbox.checked) {
                        add(name);
                    } else {
                        remove(name);
                    }

                    // Restore focus after rebuilding the options.
                    [...options.querySelectorAll("input")]
                        .find(input => input.value === name)
                        ?.focus();
                });
            }

            empty.hidden = names.length > 0;

            empty.textContent = catalog.size === 0
                ? "No saved tags yet. Add your first tag below."
                : "No matching tags. Add a new tag below.";
        }

        function render() {
            selectedList.replaceChildren();

            for (const name of selected) {
                const chip = document.createElement("span");
                chip.className = "tag-picker-chip";

                const text = document.createElement("span");
                text.textContent = name;

                const button = document.createElement("button");
                button.type = "button";
                button.className = "tag-picker-remove";
                button.textContent = "×";
                button.setAttribute("aria-label", `Remove ${name}`);

                button.addEventListener("click", () => {
                    const index = selected.indexOf(name);
                    remove(name);

                    const buttons = selectedList.querySelectorAll("button");

                    const focusTarget =
                        buttons[Math.min(index, buttons.length - 1)] ||
                        menu.querySelector("summary");

                    focusTarget.focus();
                });

                chip.append(text, button);
                selectedList.append(chip);
            }

            if (selected.length === 0) {
                const placeholder = document.createElement("span");
                placeholder.className = "tag-picker-empty";
                placeholder.textContent = "No tags selected.";
                selectedList.append(placeholder);
            }

            valueInput.value = selected.join(", ");
            count.textContent = `${selected.length} / 10`;

            renderOptions();
        }

        function add(rawName) {
            const name = normalizeWhitespace(rawName);

            if (!name) {
                showError("Enter a tag name.");
                return false;
            }

            if (name.includes(",")) {
                showError("Add one tag at a time, without commas.");
                return false;
            }

            if (name.length > 80 || normalize(name).length > 80) {
                showError("Each tag must be 80 characters or fewer.");
                return false;
            }

            if (selected.some(tag => normalize(tag) === normalize(name))) {
                showError("That tag is already selected.");
                return false;
            }

            if (selected.length >= 10) {
                showError("Use up to 10 tags. Remove a tag before adding another.");
                return false;
            }

            const displayName = catalog.get(normalize(name)) || name;

            catalog.set(normalize(name), displayName);
            selected.push(displayName);

            showError("");
            render();

            status.textContent = `${displayName} added.`;

            return true;
        }

        function remove(name) {
            selected = selected.filter(
                tag => normalize(tag) !== normalize(name)
            );

            showError("");
            render();

            status.textContent = `${name} removed.`;
        }

        function setValue(value) {
            catalog.clear();

            for (const [key, name] of savedCatalog) {
                catalog.set(key, name);
            }

            selected = [...new Map(
                (value || "")
                    .split(",")
                    .map(normalizeWhitespace)
                    .filter(Boolean)
                    .map(name => [normalize(name), name])
            ).values()];

            newTag.value = "";
            search.value = "";
            menu.open = false;
            status.textContent = "";

            showError("");
            render();
        }

        function addNewTag() {
            if (add(newTag.value)) {
                newTag.value = "";
            }

            newTag.focus();
        }

        picker.querySelector("[data-tag-add]")
            .addEventListener("click", addNewTag);

        newTag.addEventListener("input", () => showError(""));

        newTag.addEventListener("keydown", event => {
            if (event.key === "Enter") {
                event.preventDefault();
                addNewTag();
            }
        });

        search.addEventListener("input", renderOptions);

        search.addEventListener("keydown", event => {
            if (event.key === "Enter") {
                event.preventDefault();
            }
        });

        menu.addEventListener("toggle", () => {
            if (menu.open) {
                search.focus();
            }
        });

        menu.addEventListener("focusout", event => {
            if (event.relatedTarget && !menu.contains(event.relatedTarget)) {
                menu.open = false;
            }
        });

        menu.addEventListener("keydown", event => {
            if (event.key === "Escape") {
                event.preventDefault();
                event.stopPropagation();

                menu.open = false;
                menu.querySelector("summary").focus();
            }
        });

        document.addEventListener("click", event => {
            if (!menu.contains(event.target)) {
                menu.open = false;
            }
        });

        // Save also includes a typed tag if Add has not been clicked.
        picker.closest("form")?.addEventListener("submit", event => {
            if (normalizeWhitespace(newTag.value)) {
                if (!add(newTag.value)) {
                    event.preventDefault();
                    newTag.focus();
                    return;
                }

                newTag.value = "";
            }

            const hasInvalidTag = selected.some(
                name => name.length > 80 || normalize(name).length > 80
            );

            if (selected.length > 10 || hasInvalidTag) {
                event.preventDefault();

                showError("Use up to 10 tags, each 80 characters or fewer.");
                menu.querySelector("summary").focus();
            }
        });

        const modal = picker.closest(".modal");

        if (modal) {
            modal.addEventListener("show.bs.modal", event => {
                const button = event.relatedTarget;

                if (!button) {
                    return;
                }

                modal.querySelector("[data-tag-transaction-id]").value =
                    button.dataset.transactionId;

                modal.querySelector("[data-tag-description]").textContent =
                    button.dataset.description;

                setValue(button.dataset.tags);
            });

            modal.addEventListener("shown.bs.modal", () => {
                menu.querySelector("summary").focus();
            });

            modal.addEventListener("hidden.bs.modal", () => {
                setValue("");
            });
        }

        setValue(valueInput.value);

        valueInput.type = "hidden";
        picker.querySelector("[data-tag-fallback]").hidden = true;
        controls.hidden = false;
    }

    function initializeAll() {
        document.querySelectorAll("[data-tag-picker]")
            .forEach(initialize);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeAll);
    } else {
        initializeAll();
    }
})();