$(function () {

    const $root = $("html");
    const $toggle = $("#themeToggle");
    const $icon = $("#themeIcon");

    const savedTheme =
        localStorage.getItem("accountant-theme");

    const systemPrefersDark =
        window.matchMedia &&
        window.matchMedia("(prefers-color-scheme: dark)").matches;

    const initialTheme =
        savedTheme ??
        (systemPrefersDark ? "dark" : "light");

    applyTheme(initialTheme);

    $toggle.on("click", function () {

        const currentTheme =
            $root.attr("data-theme") ?? "light";

        const newTheme =
            currentTheme === "dark"
                ? "light"
                : "dark";

        applyTheme(newTheme);

        localStorage.setItem(
            "accountant-theme",
            newTheme
        );
    });

    function applyTheme(theme) {

        $root.attr("data-theme", theme);

        $icon.text(
            theme === "dark"
                ? "☀"
                : "☾"
        );
    }
});