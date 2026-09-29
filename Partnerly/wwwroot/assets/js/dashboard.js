document.addEventListener("DOMContentLoaded", function () {

    const dashboardData = window.dashboardData;

    if (!dashboardData) {
        return;
    }

    const dashboardCulture =
        window.dashboardCulture || "en-US";

    const dashboardTranslations =
        window.dashboardTranslations || {};

    const dashboardDateTranslations =
        window.dashboardDateTranslations || {
            days: [],
            months: []
        };


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

        const gamesCanvas =
            document.getElementById("gamesChart");

        if (!gamesCanvas) {
            return;
        }

        const chartScroll =
            gamesCanvas.closest(".chart-scroll");

        const chartInner =
            gamesCanvas.closest(".chart-inner");

        if (!chartScroll || !chartInner) {
            return;
        }

        const containerWidth =
            chartScroll.clientWidth;


        // =========================================
        // CALCULATE CHART WIDTH
        // =========================================

        const pointWidth =
            period === "7days"
                ? 80
                : period === "30days"
                    ? 50
                    : 70;

        const requiredWidth =
            data.length * pointWidth;

        const chartWidth =
            Math.max(
                containerWidth,
                requiredWidth
            );


        chartInner.style.width =
            `${chartWidth}px`;


        const ctx =
            gamesCanvas.getContext("2d");


        // =========================================
        // DESTROY PREVIOUS CHART
        // =========================================

        if (gamesChart) {

            gamesChart.destroy();

            gamesChart = null;
        }


        // =========================================
        // GRADIENT
        // =========================================

        const gradient =
            ctx.createLinearGradient(
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

        const labels =
            data.map(item =>
                formatDashboardDate(
                    item.date,
                    period
                )
            );


        // =========================================
        // VALUES
        // =========================================

        const values =
            data.map(
                item => item.count
            );


        // =========================================
        // CREATE CHART
        // =========================================

        gamesChart =
            new Chart(
                ctx,
                {

                    type: "line",

                    data: {

                        labels: labels,

                        datasets: [{

                            label:
                                dashboardTranslations.games,

                            data: values,

                            borderColor:
                                "#696cff",

                            backgroundColor:
                                gradient,

                            fill: true,

                            tension: 0.4,

                            borderWidth: 3,

                            pointRadius: 0,

                            pointHoverRadius: 6,

                            pointBackgroundColor:
                                "#696cff"
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

                                backgroundColor:
                                    "#233446",

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

                                    color:
                                        "#8592a3",

                                    maxRotation: 0,

                                    autoSkip: false
                                }
                            },


                            y: {

                                beginAtZero: true,

                                grid: {

                                    color:
                                        "rgba(67, 89, 113, 0.08)"
                                },

                                border: {

                                    display: false
                                },

                                ticks: {

                                    color:
                                        "#8592a3",

                                    precision: 0
                                }
                            }
                        }
                    }
                }
            );
    }


    // =========================================
    // INITIAL GAMES CHART
    // =========================================

    let currentGamesPeriod = "7days";


    const initialChartSubtitle =
        document.getElementById(
            "gamesChartSubtitle"
        );


    if (initialChartSubtitle) {

        initialChartSubtitle.textContent =
            dashboardDateTranslations.months[
            new Date().getMonth()
            ];
    }


    renderGamesChart(
        dashboardData.gamesByDay,
        currentGamesPeriod
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
                    document.getElementById(
                        "gamesChartTitle"
                    );


                const chartSubtitle =
                    document.getElementById(
                        "gamesChartSubtitle"
                    );


                if (!period) {
                    return;
                }


                // =========================================
                // SAVE CURRENT PERIOD
                // =========================================

                currentGamesPeriod =
                    period;


                // =========================================
                // UPDATE TITLE
                // =========================================

                if (chartTitle && title) {

                    chartTitle.textContent =
                        title;
                }


                if (chartSubtitle) {

                    if (
                        period === "7days" ||
                        period === "30days"
                    ) {

                        chartSubtitle.textContent =
                            dashboardDateTranslations.months[
                            new Date().getMonth()
                            ];

                    } else {

                        chartSubtitle.textContent =
                            new Date().getFullYear();
                    }
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


                this.classList.add(
                    "active"
                );


                // =========================================
                // REFRESH CHART CARD
                // =========================================

                const chart =
                    document.getElementById(
                        "gamesChart"
                    );


                const chartCard =
                    chart
                        ? chart.closest(
                            ".dashboard-card"
                        )
                        : null;


                if (chartCard) {

                    setCardRefreshing(
                        chartCard,
                        true
                    );
                }


                try {

                    const response =
                        await fetch(
                            `/Dashboard/GetGamesByPeriod?period=${encodeURIComponent(period)}`,
                            {
                                method: "GET",

                                headers: {
                                    "X-Requested-With":
                                        "XMLHttpRequest"
                                },

                                cache: "no-store"
                            }
                        );


                    if (!response.ok) {

                        throw new Error(
                            "Failed to load dashboard data."
                        );
                    }


                    const data =
                        await response.json();


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
                finally {

                    if (chartCard) {

                        setCardRefreshing(
                            chartCard,
                            false
                        );
                    }
                }
            }
        );
    });


    // =========================================
    // WINS / LOSSES CHART
    // =========================================

    let winLoseChart = null;


    function renderWinLoseChart(
        victories,
        defeats
    ) {

        const winLoseCanvas =
            document.getElementById(
                "winLoseChart"
            );


        if (!winLoseCanvas) {
            return;
        }


        if (winLoseChart) {

            winLoseChart.destroy();

            winLoseChart = null;
        }


        winLoseChart =
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

                                victories,

                                defeats
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


    renderWinLoseChart(
        dashboardData.victories,
        dashboardData.defeats
    );


    // =========================================
    // CARD REFRESH
    // =========================================

    function setCardRefreshing(
        card,
        isRefreshing
    ) {

        if (!card) {
            return;
        }


        let overlay =
            card.querySelector(
                ".dashboard-refresh-overlay"
            );


        // =========================================
        // CREATE OVERLAY
        // =========================================

        if (!overlay) {

            overlay =
                document.createElement(
                    "div"
                );


            overlay.className =
                "dashboard-refresh-overlay";


            overlay.innerHTML = `
                <div class="dashboard-refresh-spinner"></div>
            `;


            card.appendChild(
                overlay
            );
        }


        const button =
            card.querySelector(
                ".dashboard-card-refresh"
            );


        // =========================================
        // START REFRESH
        // =========================================

        if (isRefreshing) {

            card.classList.add(
                "is-refreshing"
            );


            if (button) {

                button.classList.add(
                    "is-loading"
                );


                button.disabled =
                    true;
            }
        }


        // =========================================
        // FINISH REFRESH
        // =========================================

        else {

            card.classList.remove(
                "is-refreshing"
            );


            if (button) {

                button.classList.remove(
                    "is-loading"
                );


                button.disabled =
                    false;
            }
        }
    }


    // =========================================
    // GET CARD
    // =========================================

    function getRefreshCard(button) {

        return button.closest(
            ".dashboard-stat-card, .dashboard-card"
        );
    }


    // =========================================
    // GET STATS
    // =========================================

    async function refreshDashboardCard(
        cardType,
        card
    ) {

        switch (cardType) {

            // =====================================
            // GAMES PLAYED
            // =====================================

            case "gamesPlayed": {

                const stats =
                    await getDashboardStats();


                updateStatCardValue(
                    "gamesPlayed",
                    stats.gamesPlayed
                );


                break;
            }


            // =====================================
            // VICTORIES
            // =====================================

            case "victories": {

                const stats =
                    await getDashboardStats();


                updateStatCardValue(
                    "victories",
                    stats.victories
                );


                break;
            }


            // =====================================
            // DEFEATS
            // =====================================

            case "defeats": {

                const stats =
                    await getDashboardStats();


                updateStatCardValue(
                    "defeats",
                    stats.defeats
                );


                break;
            }


            // =====================================
            // WIN RATE
            // =====================================

            case "winRate": {

                const stats =
                    await getDashboardStats();


                updateStatCardValue(
                    "winRate",
                    stats.winRate
                );


                break;
            }


            // =====================================
            // GAMES CHART
            // =====================================

            case "gamesChart": {

                const response =
                    await fetch(
                        `/Dashboard/GetGamesByPeriod?period=${encodeURIComponent(currentGamesPeriod)}`,
                        {
                            method: "GET",

                            headers: {
                                "X-Requested-With":
                                    "XMLHttpRequest"
                            },

                            cache: "no-store"
                        }
                    );


                if (!response.ok) {

                    throw new Error(
                        "Failed to refresh games chart."
                    );
                }


                const data =
                    await response.json();


                renderGamesChart(
                    data,
                    currentGamesPeriod
                );


                break;
            }


            // =====================================
            // WIN / LOSE CHART
            // =====================================

            case "winLoseChart": {

                const stats =
                    await getDashboardStats();


                renderWinLoseChart(
                    stats.victories,
                    stats.defeats
                );


                break;
            }
        }
    }

    async function getDashboardStats() {

        const response =
            await fetch(
                "/Dashboard/GetStats",
                {
                    method: "GET",

                    headers: {
                        "X-Requested-With":
                            "XMLHttpRequest"
                    },

                    cache: "no-store"
                }
            );


        if (!response.ok) {

            throw new Error(
                "Failed to load dashboard statistics."
            );
        }


        return await response.json();
    }


    // =========================================
    // UPDATE STAT CARD VALUE
    // =========================================

    function updateStatCardValue(
        cardType,
        value
    ) {

        const card =
            document.querySelector(
                `.dashboard-card-refresh[data-card="${cardType}"]`
            );


        if (!card) {
            return;
        }


        const parentCard =
            getRefreshCard(card);


        if (!parentCard) {
            return;
        }


        const valueElement =
            parentCard.querySelector(
                ".stat-value"
            );


        if (!valueElement) {
            return;
        }


        valueElement.textContent =
            value;
    }

    // =========================================
    // REFRESH BUTTON CLICK
    // =========================================

    document.addEventListener(
        "click",
        async function (event) {

            const button =
                event.target.closest(
                    ".dashboard-card-refresh"
                );


            if (!button) {
                return;
            }


            if (button.disabled) {
                return;
            }


            const card =
                getRefreshCard(button);


            if (!card) {
                return;
            }


            const cardType =
                button.dataset.card;


            if (!cardType) {
                return;
            }


            // =====================================
            // START LOADING
            // =====================================

            setCardRefreshing(
                card,
                true
            );


            try {

                await refreshDashboardCard(
                    cardType,
                    card
                );

            }
            catch (error) {

                console.error(
                    "Dashboard refresh error:",
                    error
                );
            }
            finally {

                // =================================
                // STOP LOADING
                // =================================

                setCardRefreshing(
                    card,
                    false
                );
            }
        }
    );


    // =========================================
    // WINDOW RESIZE
    // =========================================

    window.addEventListener(
        "resize",
        function () {

            if (!gamesChart) {
                return;
            }


            gamesChart.resize();
        }
    );

});