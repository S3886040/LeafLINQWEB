$(document).ready(function () {

    $("#tabPlants .plant-tab").click(function () {
        var tab_id = $(this).attr('data-tab');
        $("#tabPlants .plant-tab").removeClass('active');
        $(".tab-content-1").removeClass('active');
        $(this).addClass('active');
        $("#" + tab_id).addClass('active');
    });

    $("#tabReports .report-tab").click(function () {
        var tab_id = $(this).attr('data-tab');
        $("#tabReports .report-tab").removeClass('active');
        $(".tab-content-2").removeClass('active');
        $(this).addClass('active');
        $("#" + tab_id).addClass('active');
    });

    $("#tabSettings .settings-tab").click(function () {
        var tab_id = $(this).attr('data-tab');
        $("#tabSettings .settings-tab").removeClass('active');

        $(this).addClass('active');
        $(".tab-content-1").removeClass('active');
        $(".tab-content-2").removeClass('active');
        $("#" + tab_id).addClass('active');
    });

    $("#togglePassword").click(function () {
        var passwordInput = $("#password");
        var passwordToggle = $(".password-toggle");
        if (passwordInput.attr("type") === "password") {
            passwordInput.attr("type", "text");
            passwordToggle.text("Hide");
        } else {
            passwordInput.attr("type", "password");
            passwordToggle.text("Show");
        }
    });

    $("#sidebarMobile").click(function () {
        $(".sidebar").toggleClass("active");
        $(".mobile-overlay").toggle();
        $(".sidebar-mobile .fa-xmark").toggle();
        $(".sidebar-mobile .fa-bars").toggle();
    });

    $("#downloadDailyReports").click(function () {
        var data = [
            ["Stats", "Value"],
            ["Health State", $("#healthState").data("percentage")],
            ["Soil Moisture", $("#soilMoisture").data("percentage")],
            ["Humidity", $("#humidity").data("percentage")],
            ["Light Intensity", $("#lightIntensity").data("percentage")],

            ["Device Status", $("#connectionStatus").data("bool")],
            ["Signal Strength", $("#signalStrength").data("percentage")],
            ["Power Status", $("#powerStatus").data("bool")]
        ];

        var csvContent = "data:text/csv;charset=utf-8," + data.map(function (row) { return row.join(","); }).join("\n");

        var link = document.createElement("a");
        link.setAttribute("href", encodeURI(csvContent));
        link.setAttribute("download", "data.csv");

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    });


    $("#downloadMonthlyReports").click(function () {
        var dateDates = [];
        var temperatureData = [];
        var soilMoistureData = [];
        var humidityData = [];
        var lightIntensityData = [];

        $(".line-chart .weekly-reports").each(function () {
            var date = $(this).find("input[data-date]").attr("data-date");
            var temperature = $(this).find("input[data-temperature]").attr("data-temperature");
            var soil_moisture = $(this).find("input[data-soil-moisture]").attr("data-soil-moisture");
            var humidity = $(this).find("input[data-humidity]").attr("data-humidity");
            var light_intensity = $(this).find("input[data-light-intensity]").attr("data-light-intensity");

            dateDates.push(date);
            temperatureData.push(temperature);
            soilMoistureData.push(soil_moisture);
            humidityData.push(humidity);
            lightIntensityData.push(light_intensity);
        });

        var data = [
            ["Date", "Temperature", "Soil Moisture", "Humidity", "Light Intensity"]
        ];

        for (var i = 0; i < dateDates.length; i++) {
            data.push([dateDates[i], temperatureData[i], soilMoistureData[i], humidityData[i], lightIntensityData[i]]);
        }

        var csvContent = "data:text/csv;charset=utf-8," + data.map(function (row) { return row.join(","); }).join("\n");

        var link = document.createElement("a");
        link.setAttribute("href", encodeURI(csvContent));
        link.setAttribute("download", "monthly_reports.csv");

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    });



    $("#searchInputUsersPlants").on("keyup", function () {
        var value = $(this).val().toLowerCase();
        $(".main-table tbody tr").filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });


});
