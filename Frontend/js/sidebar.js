// ==========================================================================
// MENU ADMIN — nguồn DUY NHẤT cho menu sidebar trang quản trị.
//
// Trước đây menu được copy-paste tay vào từng file HTML nên các trang lệch
// nhau (thiếu mục, sai thứ tự). Giờ cả 10 trang đều gọi chung file này.
//
// Thêm / sửa / bỏ một mục menu → chỉ sửa MENU_ADMIN bên dưới, không đụng HTML.
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
    { href: 'readers.html',      icon: 'fa-users',              ten: 'Độc Giả' },
    { href: 'loans.html',        icon: 'fa-hand-holding-hand', ten: 'Mượn - Trả' },
    { href: 'reservations.html', icon: 'fa-calendar-check',     ten: 'Đặt Chỗ' },
    { href: 'fines.html',        icon: 'fa-money-bill-wave',    ten: 'Tiền Phạt' },
    { href: 'reports.html',      icon: 'fa-chart-bar',          ten: 'Báo Cáo' },
    { href: 'nhatky.html',       icon: 'fa-clock-rotate-left',  ten: 'Nhật Ký' }
];

// Script nằm cuối <body> nên thẻ <ul id="adminMenu"> đã được đọc xong.
(function VeMenuAdmin() {
    const ul = document.getElementById('adminMenu');
    if (!ul) return;   // trang không có sidebar admin → bỏ qua, không báo lỗi

    // 'pages/admin/books.html' → 'books.html'
    const trangHienTai = location.pathname.split('/').pop();

    ul.innerHTML = MENU_ADMIN.map(m => {
        const active = (m.href === trangHienTai) ? ' class="active"' : '';
        return `<li><a href="${m.href}"${active}>` +
               `<i class="fa-solid ${m.icon}"></i> ${m.ten}</a></li>`;
    }).join('');
})();