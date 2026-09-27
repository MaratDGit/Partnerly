
document.addEventListener("DOMContentLoaded", function () {

    const dashboardData = window.dashboardData;

    if (!dashboardData) {
        return;
    }

    const dashboardCulture =
        window.dashboardCulture || "en-US";

    const dashboardTranslations =
        window.dashboardTranslations || {};

    // =========================================
    // GAMES CHART
    // =========================================

    let gamesChart = null;

    function formatDashboardDate(dateString, period) {

        const date = new Date(dateString);

        const dayIndex = date.getDay() === 0
            ? 6
            : date.getDay() - 1;

        const monthIndex = date.getMonth();

        if (period === "7days") {

            return `${dashboardDateTranslations.days[dayIndex]}, ${date.getDate()}`;
        }

        if (period === "30days") {

            const dayName =
                dashboardDateTranslations.days[dayIndex].substring(0, 3);

            return `${dayName}, ${date.getDate()}`;
        }

        return dashboardDateTranslations.months[monthIndex];
    }

    function renderGamesChart(data, period) {

        const gamesCanvas = document.getElementById("gamesChart");

        if (!gamesCanvas) {
            return;
        }

        const chartScroll = gamesCanvas.closest(".chart-scroll");
        const chartInner = gamesCanvas.closest(".chart-inner");

        if (!chartScroll || !chartInner) {
            return;
        }

        const containerWidth = chartScroll.clientWidth;

        // Width of one data point
        const pointWidth =
            period === "7days"
                ? 80
                : period === "30days"
                    ? 50
                    : 70;

        const requiredWidth =
            data.length * pointWidth;

        const chartWidth =
            Math.max(containerWidth, requiredWidth);

        // Set inner container width
        chartInner.style.width = `${chartWidth}px`;

        const ctx = gamesCanvas.getContext("2d");


        // Destroy previous chart
        if (gamesChart) {
            gamesChart.destroy();
        }


        // =========================================
        // GRADIENT
        // =========================================

        const gradient = ctx.createLinearGradient(
            0,
            0,
            0,
            300
        );

        gradient.addColorStop(
            0,
            "rgba(105, 108, 255, 0.30)"
        );

        gradient.addColorStop(
            1,
            "rgba(105, 108, 255, 0)"
        );


        // =========================================
        // LABELS
        // =========================================

        const labels = data.map(item =>
            formatDashboardDate(item.date, period)
        );


        // =========================================
        // VALUES
        // =========================================

        const values = data.map(
            item => item.count
        );


        // =========================================
        // CREATE CHART
        // =========================================

        gamesChart = new Chart(ctx, {

            type: "line",


            data: {

                labels: labels,

                datasets: [{

                    label: dashboardTranslations.games,

                    data: values,

                    borderColor: "#696cff",

                    backgroundColor: gradient,

                    fill: true,

                    tension: 0.4,

                    borderWidth: 3,

                    pointRadius: 0,

                    pointHoverRadius: 6,

                    pointBackgroundColor: "#696cff"
                }]
            },


            options: {

                responsive: true,

                maintainAspectRatio: false,


                interaction: {

                    intersect: false,

                    mode: "index"
                },


                plugins: {

                    legend: {

                        display: false
                    },


                    tooltip: {

                        backgroundColor: "#233446",

                        padding: 12,

                        cornerRadius: 8,

                        displayColors: false
                    }
                },


                scales: {
                    x: {
                        grid: {
                            display: false
                        },

                        border: {
                            display: false
                        },

                        ticks: {
                            color: "#8592a3",
                            maxRotation: 0,
                            autoSkip: false
                        }
                    },

                    y: {
                        beginAtZero: true,

                        grid: {
                            color: "rgba(67, 89, 113, 0.08)"
                        },

                        border: {
                            display: false
                        },

                        ticks: {
                            color: "#8592a3",
                            precision: 0
                        }
                    }
                }
            }
        });
    }


    // =========================================
    // INITIAL GAMES CHART
    // =========================================

    const initialPeriod = "7days";

    const initialChartSubtitle =
        document.getElementById("gamesChartSubtitle");

    if (initialChartSubtitle) {

        initialChartSubtitle.textContent =
            dashboardDateTranslations.months[new Date().getMonth()];
    }

    renderGamesChart(
        dashboardData.gamesByDay,
        initialPeriod
    );


    // =========================================
    // PERIOD DROPDOWN
    // =========================================

    const periodOptions =
        document.querySelectorAll(
            ".games-period-option"
        );


    const selectedPeriod =
        document.getElementById(
            "selectedGamesPeriod"
        );


    periodOptions.forEach(option => {

        option.addEventListener(
            "click",
            async function () {

                const period =
                    this.dataset.period;


                const label =
                    this.dataset.label;

                const title =
                    this.dataset.title;

                const chartTitle =
                    document.getElementById("gamesChartTitle");

                const chartSubtitle =
                    document.getElementById("gamesChartSubtitle");

                if (chartTitle && title) {
                    chartTitle.textContent = title;
                }

                if (chartSubtitle) {

                    if (period === "7days" || period === "30days") {

                        chartSubtitle.textContent =
                            dashboardDateTranslations.months[new Date().getMonth()];

                    } else {

                        chartSubtitle.textContent =
                            new Date().getFullYear();
                    }
                }

                if (!period) {
                    return;
                }


                // =========================================
                // UPDATE SELECTED TEXT
                // =========================================

                if (selectedPeriod) {

                    selectedPeriod.textContent =
                        label;
                }


                // =========================================
                // UPDATE ACTIVE OPTION
                // =========================================

                periodOptions.forEach(item => {

                    item.classList.remove(
                        "active"
                    );
                });


                this.classList.add("active");


                // =========================================
                // LOAD DATA
                // =========================================

                try {

                    const response =
                        await fetch(
                            `/Dashboard/GetGamesByPeriod?period=${encodeURIComponent(period)}`
                        );


                    if (!response.ok) {

                        throw new Error(
                            "Failed to load dashboard data."
                        );
                    }


                    const data =
                        await response.json();


                    // =========================================
                    // UPDATE CHART
                    // =========================================

                    renderGamesChart(
                        data,
                        period
                    );

                }
                catch (error) {

                    console.error(
                        "Dashboard chart error:",
                        error
                    );
                }
            }
        );
    });


    // =========================================
    // WINS / LOSSES
    // =========================================

    const winLoseCanvas =
        document.getElementById(
            "winLoseChart"
        );


    if (winLoseCanvas) {

        new Chart(
            winLoseCanvas,
            {

                type: "doughnut",


                data: {

                    labels: [
                        dashboardTranslations.victories,
                        dashboardTranslations.defeats
                    ],


                    datasets: [{

                        data: [

                            dashboardData.victories,

                            dashboardData.defeats
                        ],


                        backgroundColor: [

                            "#28c76f",

                            "#ff4c51"
                        ],


                        borderWidth: 0,


                        hoverOffset: 6
                    }]
                },


                options: {

                    responsive: true,

                    maintainAspectRatio: false,

                    cutout: "72%",


                    plugins: {

                        legend: {

                            display: false
                        },


                        tooltip: {

                            backgroundColor:
                                "#233446",

                            padding: 12,

                            cornerRadius: 8
                        }
                    }
                }
            }
        );
    }

});