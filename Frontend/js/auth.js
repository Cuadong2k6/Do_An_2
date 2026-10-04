document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('loginForm');
    
    // Nếu ở trang login
    if (loginForm) {
        // Đã đăng nhập rồi → chuyển thẳng sang trang theo vai trò
        // (nếu không, bạn đọc sẽ bị đá vòng vô tới dashboard rồi quay lại login → lặp)
        if (localStorage.getItem('token')) {
            const role = (JSON.parse(localStorage.getItem('user') || '{}').role || '').toLowerCase();
            const laBanDoc = role === 'bandoc' || role === 'reader';
            window.location.href = window.location.origin
                + (laBanDoc ? '/pages/reader/search.html' : '/pages/admin/dashboard.html');
        }

        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();

            const tendangnhap = document.getElementById('tendangnhap').value.trim();
            const password    = document.getElementById('password').value;
            const btnSubmit   = loginForm.querySelector('button[type="submit"]');
            const errorDiv    = document.getElementById('loginError');

            // Trạng thái loading
            const originalText = btnSubmit.innerHTML;
            btnSubmit.innerHTML = 'Đang đăng nhập...';
            btnSubmit.disabled = true;

            try {
                // Backend tự tra users (Admin / Thủ thư) trước, không có thì tra readers (Bạn đọc)
                const res = await window.api.post('/auth/login', { tendangnhap: tendangnhap, matkhau: password });

                if (res.success && res.data && res.data.token) {
                    localStorage.setItem('token', res.data.token);
                    localStorage.setItem('user', JSON.stringify({
                        role: res.data.role,
                        name: res.data.hoten,
                        id: res.data.user_id
                    }));

                    // Bạn đọc → trang tìm kiếm; nhân viên → dashboard
                    const laBanDoc = (res.data.role || '').toLowerCase() === 'bandoc';
                    window.location.href = window.location.origin
                        + (laBanDoc ? '/pages/reader/search.html' : '/pages/admin/dashboard.html');
                } else {
                    throw new Error(res.message || 'Đăng nhập thất bại!');
                }
            } catch (error) {
                errorDiv.textContent = error.message || "Đăng nhập thất bại!";
                errorDiv.style.display = 'block';
            } finally {
                btnSubmit.innerHTML = originalText;
                btnSubmit.disabled = false;
            }
        });
    }

    // Xử lý nút Đăng xuất (trên các trang dashboard)
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', (e) => {
            e.preventDefault();
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            window.location.href = window.location.origin + '/index.html'; // Về trang chủ
        });
    }
});

// Hàm kiểm tra bảo vệ các trang
function YeuCauXacThuc(allowedRoles = []) {
    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user') || '{}');

    if (!token) {
        window.location.href = window.location.origin + '/index.html';
        return;
    }

    if (allowedRoles.length > 0) {
        const userRole = (user.role || '').toLowerCase();
        const allowed  = allowedRoles.map(r => r.toLowerCase());
        if (!allowed.includes(userRole)) {
            alert("Bạn không có quyền truy cập trang này!");
            window.location.href = window.location.origin + '/index.html';
        }
    }
}

// Helper: kiểm tra role hiện tại có phải Admin hay không
function isAdmin() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    return (user.role || '').toLowerCase() === 'admin';
}
