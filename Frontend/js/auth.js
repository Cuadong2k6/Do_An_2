document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('loginForm');

    const googleButton = document.getElementById('googleSignInButton');
    const googleError = document.getElementById('loginError');
    if (googleButton) {
        window.api.get('/auth/google-config')
            .then(config => {
                const batDauGoogle = () => {
                    if (!window.google?.accounts?.id || !config.client_id) {
                        googleError.textContent = 'Đăng nhập Google chưa được cấu hình.';
                        googleError.style.display = 'block';
                        return;
                    }

                    google.accounts.id.initialize({
                        client_id: config.client_id,
                        callback: async ({ credential }) => {
                            try {
                        const result = await window.api.post('/auth/google-login', { id_token: credential });
                                if (!result.success || !result.data?.token) throw new Error(result.message || 'Đăng nhập Google thất bại.');
                                localStorage.setItem('token', result.data.token);
                                localStorage.setItem('user', JSON.stringify({
                                    role: result.data.role,
                                    name: result.data.hoten,
                                    id: result.data.user_id,
                                    onboarding_status: result.data.onboarding_status
                                }));
                                window.location.href = '/pages/reader/search.html';
                            } catch (error) {
                                googleError.textContent = error.message || 'Đăng nhập Google thất bại.';
                                googleError.style.display = 'block';
                            }
                        }
                    });
                    google.accounts.id.renderButton(googleButton, { theme: 'outline', size: 'large', text: 'continue_with' });
                };

                const batDauChoThoiGian = Date.now();
                const chayChoGoogle = () => {
                    if (window.google?.accounts?.id) return batDauGoogle();
                    if (Date.now() - batDauChoThoiGian > 10000) {
                        googleError.textContent = 'Không tải được dịch vụ Google. Hãy tải lại trang để thử lại.';
                        googleError.style.display = 'block';
                        return;
                    }
                    setTimeout(chayChoGoogle, 100);
                };
                chayChoGoogle();
            })
            .catch(() => {
                googleError.textContent = 'Không tải được cấu hình đăng nhập Google.';
                googleError.style.display = 'block';
            });
    }

    // Nếu ở trang login
    if (loginForm) {
        // Đã đăng nhập rồi → chuyển thẳng sang trang theo vai trò
        // (nếu không, bạn đọc sẽ bị đá vòng vô tới dashboard rồi quay lại login → lặp)
        if (localStorage.getItem('token')) {
            const role = (JSON.parse(localStorage.getItem('user') || '{}').role || '').toLowerCase();

            if (role === 'bandoc' || role === 'reader' || role === 'user') {
                window.location.href = window.location.origin + '/pages/reader/search.html';
            } else if (role === 'thuthu') {
                window.location.href = window.location.origin + '/pages/librarian/dashboard.html';
            } else {
                window.location.href = window.location.origin + '/pages/admin/dashboard.html';
            }
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

                    // Chuyển hướng theo vai trò sau khi đăng nhập thành công
                    const r = (res.data.role || '').toLowerCase();
                    if (r === 'bandoc' || r === 'reader' || r === 'user') {
                        window.location.href = window.location.origin + '/pages/reader/search.html';
                    } else if (r === 'thuthu') {
                        window.location.href = window.location.origin + '/pages/librarian/dashboard.html';
                    } else {
                        window.location.href = window.location.origin + '/pages/admin/dashboard.html';
                    }
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
