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
            
            const loginType = window.currentLoginType || 'admin';
            const password = document.getElementById('password').value;
            const btnSubmit = loginForm.querySelector('button[type="submit"]');
            const errorDiv = document.getElementById('loginError');
            
            // Trạng thái loading
            const originalText = btnSubmit.innerHTML;
            btnSubmit.innerHTML = 'Đang đăng nhập...';
            btnSubmit.disabled = true;
            
            try {
                let res;
                if (loginType === 'admin') {
                    const username = document.getElementById('username').value;
                    // Backend nhận { taikhoan, matkhau } — Response: { success, data: { token, hoten, role, ... } }
                    res = await window.api.post('/auth/login-admin', { taikhoan: username, matkhau: password });
                } else {
                    const email = document.getElementById('email').value;
                    // Backend nhận { email, matkhau } — Response: { success, data: { reader, token, ... } }
                    res = await window.api.post('/auth/login-reader', { email: email, matkhau: password });
                }
                
                if (res.success && res.data && res.data.token) {
                    localStorage.setItem('token', res.data.token);
                    let userInfo;
                    if (loginType === 'admin') {
                        userInfo = {
                            role: res.data.role,
                            name: res.data.hoten,
                            id: res.data.user_id
                        };
                    } else {
                        userInfo = {
                            role: 'reader',
                            name: res.data.reader.hoten,
                            id: res.data.reader.reader_id
                        };
                    }
                    localStorage.setItem('user', JSON.stringify(userInfo));
                    window.location.href = loginType === 'admin' ? 'pages/admin/dashboard.html' : '../pages/reader/search.html';
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
function YeuCauXacThuc(allowedRoles = []) {
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

// Helper: kiểm tra role hiện tại có phải Admin hay không
function isAdmin() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    return (user.role || '').toLowerCase() === 'admin';
}
