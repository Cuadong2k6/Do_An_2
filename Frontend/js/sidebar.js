// ==========================================================================
// MENU ADMIN & LIBRARIAN — nguồn DUY NHẤT cho menu sidebar trang quản trị.
//
// Mọi chuỗi ở đây đều là hằng số viết tay trong code, KHÔNG có dữ liệu từ
// người dùng hay từ URL nào được chèn vào HTML → không có đường XSS.
// location.pathname chỉ dùng để so sánh (===), không đưa vào chuỗi HTML.
// ==========================================================================

const MENU_ADMIN = [
    { href: 'dashboard.html',    icon: 'fa-chart-line',         ten: 'Tổng Quan' },
    { href: 'books.html',        icon: 'fa-book',               ten: 'Quản Lý Sách' },
    { href: 'copies.html',       icon: 'fa-copy',               ten: 'Bản Sao Sách' },
    { href: 'shelves.html',      icon: 'fa-warehouse',          ten: 'Kệ Sách' },
    { href: 'accounts.html',     icon: 'fa-users',              ten: 'Tài Khoản' },
    { href: 'loans.html',        icon: 'fa-hand-holding-hand', ten: 'Mượn - Trả' },
    { href: 'reservations.html', icon: 'fa-calendar-check',     ten: 'Đặt Chỗ' },
    { href: 'fines.html',        icon: 'fa-money-bill-wave',    ten: 'Tiền Phạt' },
    { href: 'reports.html',      icon: 'fa-chart-bar',          ten: 'Báo Cáo' },
    { href: 'nhatky.html',       icon: 'fa-clock-rotate-left',  ten: 'Nhật Ký' }
];

const MENU_LIBRARIAN = [
    { href: 'dashboard.html',    icon: 'fa-chart-line',         ten: 'Tổng Quan' },
    { href: 'books.html',        icon: 'fa-book',               ten: 'Quản Lý Sách' },
    { href: 'copies.html',       icon: 'fa-copy',               ten: 'Bản Sao Sách' },
    { href: 'readers.html',      icon: 'fa-users',              ten: 'Độc Giả' },
    { href: 'loans.html',        icon: 'fa-hand-holding-hand', ten: 'Mượn - Trả' },
    { href: 'reservations.html', icon: 'fa-calendar-check',     ten: 'Đặt Chỗ' },
    { href: 'fines.html',        icon: 'fa-money-bill-wave',    ten: 'Tiền Phạt' },
    { href: 'reports.html',      icon: 'fa-chart-bar',          ten: 'Báo Cáo' }
];

// Script nằm cuối <body> — tự chọn đúng <ul> theo trang.
(function VeMenu() {
    const isLibrarian = location.pathname.includes('/librarian/');
    const ul = document.getElementById(isLibrarian ? 'librarianMenu' : 'adminMenu');
    if (!ul) return;

    const trangHienTai = location.pathname.split('/').pop();
    const menuToUse = isLibrarian ? MENU_LIBRARIAN : MENU_ADMIN;

    ul.innerHTML = menuToUse.map(m => {
        const active = (m.href === trangHienTai) ? ' class="active"' : '';
        return '<li><a href="' + m.href + '"' + active + '>' +
               '<i class="fa-solid ' + m.icon + '"></i> ' + m.ten + '</a></li>';
    }).join('');
})();

// Page guard - bảo vệ các trang admin/librarian/reader
(function PageGuard() {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.replace('/index.html');
        return;
    }

    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const role = (user.role || '').toLowerCase();
    const currentPath = window.location.pathname;

    if (currentPath.includes('/admin/') && role !== 'admin') {
        window.location.replace('/index.html');
        return;
    }

    if (currentPath.includes('/librarian/') && role !== 'thuthu') {
        window.location.replace('/index.html');
        return;
    }

    if (currentPath.includes('/reader/') && role !== 'user' && role !== 'bandoc' && role !== 'reader') {
        window.location.replace('/index.html');
        return;
    }
})();
