(function () {
    // Swashbuckle inyecta este script en <head>, antes de que <body> exista.
    // SwaggerUI v5 renderiza el topbar con React después del parse del HTML,
    // y el contenido del link es un <svg> inline (antes era <img>).
    // Observamos document.documentElement (siempre presente) y reemplazamos el
    // logo + link en cuanto el topbar aparezca en el DOM.
    function applyBranding() {
        var topbarLink = document.querySelector(".swagger-ui .topbar a");
        if (!topbarLink) return false;

        topbarLink.href = "https://www.tech-bi.com";
        topbarLink.target = "_blank";

        var img = document.createElement("img");
        img.src = "images/logo-white.png";
        img.alt = "API - ACG Fotos";
        topbarLink.replaceChildren(img);
        return true;
    }

    if (applyBranding()) return;

    var observer = new MutationObserver(function () {
        if (applyBranding()) observer.disconnect();
    });
    observer.observe(document.documentElement, { childList: true, subtree: true });
})();
