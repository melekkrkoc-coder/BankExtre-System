function drawDonutChart(canvasId, labels, values, colors, centerText) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');

    // Canvas çözünürlüğü
    const dpr = window.devicePixelRatio || 1;
    const width = parseInt(canvas.getAttribute('width')) || 220;
    const height = parseInt(canvas.getAttribute('height')) || 220;

    canvas.width = width * dpr;
    canvas.height = height * dpr;
    canvas.style.width = `${width}px`;
    canvas.style.height = `${height}px`;
    ctx.scale(dpr, dpr);

    const total = values.reduce((acc, v) => acc + v, 0);
    const centerX = width / 2;
    const centerY = height / 2;
    const radius = Math.min(centerX, centerY) - 15;
    const innerRadius = radius * 0.68;

    ctx.clearRect(0, 0, width, height);

    if (total === 0) {
        ctx.beginPath();
        ctx.arc(centerX, centerY, radius, 0, 2 * Math.PI);
        ctx.arc(centerX, centerY, innerRadius, 2 * Math.PI, 0, true);
        ctx.fillStyle = '#e2e8f0';
        ctx.fill();
        return;
    }

    let startAngle = -0.5 * Math.PI;

    for (let i = 0; i < values.length; i++) {
        if (values[i] <= 0) continue;
        const sliceAngle = (values[i] / total) * 2 * Math.PI;
        const endAngle = startAngle + sliceAngle;

        ctx.beginPath();
        ctx.arc(centerX, centerY, radius, startAngle, endAngle);
        ctx.arc(centerX, centerY, innerRadius, endAngle, startAngle, true);
        ctx.closePath();
        // Düzeltilen yer: .length (küçük l harfi)
        ctx.fillStyle = colors[i % colors.length];
        ctx.fill();

        startAngle = endAngle;
    }

    // HTML Entity temizleme (&#x20BA; -> ₺)
    const decodeHtml = (html) => {
        const txt = document.createElement("textarea");
        txt.innerHTML = html;
        return txt.value;
    };
    const cleanCenterText = decodeHtml(centerText || '');

    // Koyu / Açık tema metin renkleri
    const isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';

    // 1. "Toplam Gider" Başlığı
    ctx.font = '500 11px Inter, system-ui, sans-serif';
    ctx.fillStyle = isDark ? '#94a3b8' : '#64748b';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('Toplam Gider', centerX, centerY - 10);

    // 2. Net Tutar Yazısı
    ctx.font = 'bold 15px Inter, system-ui, sans-serif';
    ctx.fillStyle = isDark ? '#f8fafc' : '#0f172a';
    ctx.fillText(cleanCenterText, centerX, centerY + 11);
}