const gamesCtx = document.getElementById('gamesChart');
new Chart(gamesCtx, {
    type: 'line',
    data: {
        labels: ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'],
        datasets: [{
            label: 'Games Count',
            data: [3, 20, 2, 20, 4, 35, 15],
            borderColor: 'blue',
            tension: 0.3
        }]
    }
});

const winLoseCtx = document.getElementById('winLoseChart');
new Chart(winLoseCtx, {
    type: 'doughnut',
    data: {
        labels: ['Win', 'Lose'],
        datasets: [{
            data: [74, 54],
            backgroundColor: ['#198754', '#dc3545']
        }]
    }
});