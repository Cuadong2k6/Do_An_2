// ==========================================================================
// Authentication Logic
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('loginForm');
    
    // Nếu ở trang login
    if (loginForm) {
        // Kiểm tra xem đã đăng nhập chưa
        if (localStorage.getItem('token')) {
            window.location.href = '/pages/admin/dashboard.html';
        }

        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;
            const btnSubmit = loginForm.querySelector('button[type="submit"]');
            const errorDiv = document.getElementById('loginError');
            
            // Trạng thái loading
            const originalText = btnSubmit.innerHTML;
            btnSubmit.innerHTML = 'Đang đăng nhập...';
            btnSubmit.disabled = true;
            
            try {
                // Backend nhận { taikhoan, matkhau } — Response: { success, data: { token, hoten, role, ... } }
                const res = await window.api.post('/Auth/login-admin', { taikhoan: email, matkhau: password });
                
                if (res.success && res.data && res.data.token) {
                    localStorage.setItem('token', res.data.token);
                    localStorage.setItem('user', JSON.stringify({
                        role: res.data.role,
                        name: res.data.hoten,
                        id: res.data.user_id
                    }));
                    window.location.href = 'pages/admin/dashboard.html';
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
            window.location.href = '../../index.html'; // Về trang chủ từ pages/admin/...
        });
    }
});

// Hàm kiểm tra bảo vệ các trang
function requireAuth(allowedRoles = []) {
    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user') || '{}');

    if (!token) {
        window.location.href = '../../index.html';
        return;
    }

    if (allowedRoles.length > 0) {
        const userRole = (user.role || '').toLowerCase();
        const allowed  = allowedRoles.map(r => r.toLowerCase());
        if (!allowed.includes(userRole)) {
            alert("Bạn không có quyền truy cập trang này!");
            window.location.href = '../../index.html';
        }
    }
}
