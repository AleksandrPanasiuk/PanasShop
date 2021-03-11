$(function () {

    /* Confirm page deletion*/
    $("a.delete").click(function () {
        if (!confirm("Confirm page deletion")) return false;
    });

    $("table#pages tbody").sortable({
        items: "tr:not(.home)",
        placeholder: "ui-state-highlight",
        update: function () {
            var ids = $("table#pages tbody").sortable("serialize");
            var url = "/Admin/Pages/ReorederPages";

            $.post(url, ids, function (data) {
            });
        }
    });
});