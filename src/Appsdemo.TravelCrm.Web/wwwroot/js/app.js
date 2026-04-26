/* Appsdemo Travel CRM front-end glue */
(function () {
    document.addEventListener('htmx:responseError', function (evt) {
        console.error('HTMX error:', evt.detail);
    });
})();
