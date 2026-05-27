$(function () {
    function normalize(value) {
        return (value || "").toString().toLowerCase().trim();
    }

    $(".js-table-search").on("input", function () {
        const tableId = $(this).data("table-id");
        const query = normalize($(this).val());
        const $table = $("#" + tableId);

        if ($table.length === 0) {
            return;
        }

        $table.find("tbody tr").each(function () {
            const rowText = normalize($(this).text());
            const matched = query.length === 0 || rowText.includes(query);
            $(this).toggle(matched);
        });
    });

    const path = window.location.pathname.toLowerCase();
    $(".app-navbar .nav-link").each(function () {
        const href = ($(this).attr("href") || "").toLowerCase();
        if (href !== "/" && path.startsWith(href)) {
            $(this).addClass("active");
        } else if (href === "/" && path === "/") {
            $(this).addClass("active");
        }
    });
});
