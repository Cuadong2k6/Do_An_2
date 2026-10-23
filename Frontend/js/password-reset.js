document.addEventListener('DOMContentLoaded', () => {
    const forgotForm = document.getElementById('forgotForm');
    const resetForm = document.getElementById('resetForm');

    if (forgotForm) {
        forgotForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const email = document.getElementById('email').value.trim();
            const btn = forgotForm.querySelector('button[type="submit"]');
            const original = btn.innerHTML;
            btn.innerHTML = 'Đang gửi...';
            btn.disabled = true;
            anthongbao();

            try {
                const res = await window.api.post('/auth/forgot-password', { email });
                if (!res || res.success !== true) {
                    hienthi('formError', (res && res.message) || 'Không gửi được yêu cầu. Thử lại sau.');
                    return;
                }
                hienthi('formOk', res.message || 'Nếu email tồn tại, hệ thống đã gửi hướng dẫn đặt lại mật khẩu.');
                forgotForm.reset();
            } catch (err) {
                hienthi('formError', (err && err.message) || 'Không gửi được yêu cầu. Thử lại sau.');
            } finally {
                btn.innerHTML = original;
                btn.disabled = false;
            }
        });
    }

    if (resetForm) {
        // Chỉ đọc token từ query. Không lưu, không ghi log.
        const token = new URLSearchParams(window.location.search).get('token') || '';
        if (!token) {
            hienthi('formError', 'Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.');
            resetForm.querySelector('button[type="submit"]').disabled = true;
        }

        resetForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const matkhau = document.getElementById('matkhau').value;
            const matkhau2 = document.getElementById('matkhau2').value;
            anthongbao();

            if (!matkhau) {
                hienthi('formError', 'Mật khẩu không hợp lệ.');
                return;
            }
            if (matkhau !== matkhau2) {
                hienthi('formError', 'Mật khẩu nhập lại không khớp.');
                return;
            }

            const btn = resetForm.querySelector('button[type="submit"]');
            const original = btn.innerHTML;
            btn.innerHTML = 'Đang lưu...';
            btn.disabled = true;

            let ok = false;
            try {
                const res = await window.api.post('/auth/reset-password', { token, matkhau });
                if (!res || res.success !== true) {
                    hienthi('formError', (res && res.message) || 'Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.');
                    return;
                }
                hienthi('formOk', res.message || 'Đặt lại mật khẩu thành công.');
                resetForm.reset();
                btn.innerHTML = 'Đã lưu';
                ok = true;
            } catch (err) {
                hienthi('formError', (err && err.message) || 'Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.');
            } finally {
                if (!ok) {
                    btn.innerHTML = original;
                    btn.disabled = false;
                }
            }
        });
    }
});

function hienthi(id, text) {
    const el = document.getElementById(id);
    if (!el) return;
    el.textContent = text;
    el.style.display = 'block';
}

function anthongbao() {
    ['formError', 'formOk'].forEach((id) => {
        const el = document.getElementById(id);
        if (!el) return;
        el.style.display = 'none';
        el.textContent = '';
    });
}
