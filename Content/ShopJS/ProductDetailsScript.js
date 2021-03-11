$(function () {
    $(".fancybox").fancybox();
    var measurementError = 30;
    $("[href='#product_description']").click(function (e) {
        e.preventDefault();
        $('html,body').animate({ scrollTop: $('.product-description').offset().top + "px" }, { duration: 500 });
    });
    $("[href='#product-reviews']").click(function (e) {
        e.preventDefault();
        $('html,body').animate({ scrollTop: $('.fancyboxdiv').offset().top + "px" }, { duration: 500 });
    });
});