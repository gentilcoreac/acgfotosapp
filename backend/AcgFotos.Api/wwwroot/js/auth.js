(function () {
    $(function () {
        addAuthorization();
    });

    function addAuthorization() {
        var data = {};
        data.username = '';
        data.password = '';
        //data.tenantId = 2;

        if (!data.username?.trim() || !data.password?.trim()) {
            return;
        }

        var url = "/api/auth/token";

        $.ajax({
            type: "POST",
            url: url,
            data: JSON.stringify(data),
            contentType: "application/json-patch+json",
            success: function (result) {
                setTimeout(function () {
                    ui.preauthorizeApiKey("Bearer", "bearer " + result.token);
                    console.log("Login exitoso!");
                }, 1000);

            }
        });
    }
})();