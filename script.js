// UI interactions
document.addEventListener('DOMContentLoaded', () => {
    // Dark mode toggle (persist in localStorage)
    const darkBtn = document.getElementById('darkModeBtn');
    const stored = localStorage.getItem('ui-theme');
    if (stored === 'dark') document.body.classList.add('dark');
    darkBtn.addEventListener('click', () => {
        document.body.classList.toggle('dark');
        const isDark = document.body.classList.contains('dark');
        localStorage.setItem('ui-theme', isDark ? 'dark' : 'light');
        darkBtn.setAttribute('aria-pressed', isDark);
    });

    // Fade-in on scroll
    const faders = document.querySelectorAll('.fade');
    const onScroll = () => {
        faders.forEach(el => {
            const rect = el.getBoundingClientRect();
            if (rect.top < window.innerHeight - 80) el.classList.add('show');
        });
    };
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();

    // Mobile menu
    const menuBtn = document.getElementById('menuBtn');
    const sidebar = document.getElementById('sidebar');
    menuBtn.addEventListener('click', () => {
        sidebar.classList.toggle('open');
    });
});
