// Guard inline — chạy ngay trước render
// Copy vào <head> của mọi trang cần bảo vệ
(function(){
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.replace('/index.html');
        return;
    }

    // Kiểm tra quyền truy cập theo vai trò
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const role = (user.role || '').toLowerCase();
    const currentPath = window.location.pathname;

    // Admin pages (trang admin)
    if (currentPath.includes('/admin/')) {
        if (!['admin', 'quản trị', 'staff'].includes(role)) {
            window.location.replace('/index.html');
            return;
        }
    }

    // Librarian pages (trang thủ thư)
    if (currentPath.includes('/librarian/')) {
        if (role !== 'thuthu') {
            window.location.replace('/index.html');
            return;
        }
    }

    // Reader pages (trang bạn đọc)
    if (currentPath.includes('/reader/')) {
        if (!['bandoc', 'reader', 'bạn đọc'].includes(role)) {
            window.location.replace('/index.html');
            return;
        }
    }
})();
