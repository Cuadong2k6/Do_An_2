// ==========================================================================
// Logic cho trang Xác thực OTP (email only, 6 chữ số)
// Không lưu token/proof vào localStorage/sessionStorage/console.
// ==========================================================================
document.addEventListener('DOMContentLoaded', () => {
    const formEmail   = document.getElementById('formEmail');
    const formOtp     = document.getElementById('formOtp');
    const ketQua      = document.getElementById('ketQua');
    const inputEmail  = document.getElementById('inputEmail');
    const inputOtp    = document.getElementById('inputOtp');
    const btnGuiOtp   = document.getElementById('btnGuiOtp');
    const btnXacThuc  = document.getElementById('btnXacThuc');
    const btnDoiEmail = document.getElementById('btnDoiEmail');
    const btnGuiLai   = document.getElementById('btnGuiLai');
    const demNguocEl  = document.getElementById('demNguoc');
    const hienThiEmail = document.getElementById('hienThiEmail');
    const stepMsg     = document.getElementById('stepMsg');

    // Biến nội bộ, KHÔNG phơi ra DOM hay storage
    let emailHienTai = '';
    let timerId = null;

    /** Hiển thị thông báo dùng textContent + aria-live */
    function HienThongBao(loai, noiDung) {
        stepMsg.textContent = noiDung;
        stepMsg.className = 'msg-box ' + (loai === 'error' ? 'msg-error' : 'msg-ok');
        stepMsg.style.display = 'block';
    }

    function AnThongBao() {
        stepMsg.style.display = 'none';
        stepMsg.textContent = '';
    }

    /** Chuyển bước UI */
    function ChuyenBuoc(buoc) {
        formEmail.classList.toggle('hidden', buoc !== 1);
        formOtp.classList.toggle('hidden', buoc !== 2);
        ketQua.classList.toggle('hidden', buoc !== 3);
    }

    /** Đếm ngược cooldown gửi lại OTP (backend quy định 60s) */
    function BatDauDemNguoc(giay) {
        let conLai = giay;
        btnGuiLai.disabled = true;
        demNguocEl.textContent = conLai;
        if (timerId) clearInterval(timerId);
        timerId = setInterval(() => {
            conLai--;
            demNguocEl.textContent = conLai;
            if (conLai <= 0) {
                clearInterval(timerId);
                timerId = null;
                btnGuiLai.disabled = false;
                demNguocEl.textContent = '0';
            }
        }, 1000);
    }

    /** Gọi API request OTP */
    async function GuiOtpAsync(email) {
        // GoiAPI ném lỗi khi non-2xx → try/catch bên ngoài bắt
        return await window.api.post('/auth/otp/request', { email: email });
    }

    /** Gọi API verify OTP */
    async function XacThucOtpAsync(email, otp) {
        return await window.api.post('/auth/otp/verify', { email: email, otp: otp });
    }

    // --- Bước 1: Gửi email nhận OTP ---
    formEmail.addEventListener('submit', async (e) => {
        e.preventDefault();
        AnThongBao();

        emailHienTai = inputEmail.value.trim();
        if (!emailHienTai) return;

        btnGuiOtp.disabled = true;
        btnGuiOtp.textContent = 'Đang gửi...';

        try {
            await GuiOtpAsync(emailHienTai);
            // Thành công → chuyển bước 2
            hienThiEmail.textContent = emailHienTai;
            ChuyenBuoc(2);
            BatDauDemNguoc(60);
            inputOtp.value = '';
            inputOtp.focus();
        } catch (err) {
            // api.js ném lỗi có message từ backend ResponseModel
            HienThongBao('error', err.message || 'Không thể gửi OTP. Vui lòng thử lại.');
        } finally {
            btnGuiOtp.disabled = false;
            btnGuiOtp.textContent = 'Gửi mã OTP';
        }
    });

    // --- Bước 2: Nhập OTP và xác thực ---
    // Chỉ cho phép nhập số, giữ leading zero
    inputOtp.addEventListener('input', () => {
        inputOtp.value = inputOtp.value.replace(/[^0-9]/g, '').slice(0, 6);
    });

    formOtp.addEventListener('submit', async (e) => {
        e.preventDefault();
        AnThongBao();

        const otp = inputOtp.value;
        if (otp.length !== 6) {
            HienThongBao('error', 'Mã OTP phải gồm đúng 6 chữ số.');
            return;
        }

        btnXacThuc.disabled = true;
        btnXacThuc.textContent = 'Đang xác thực...';

        try {
            await XacThucOtpAsync(emailHienTai, otp);
            // Không giữ res.data (proof) vì chưa có endpoint đăng ký sử dụng
            // Thành công → chuyển UI sang bước 3
            ChuyenBuoc(3);
            if (timerId) { clearInterval(timerId); timerId = null; }
        } catch (err) {
            HienThongBao('error', err.message || 'OTP không hợp lệ hoặc đã hết hạn.');
        } finally {
            btnXacThuc.disabled = false;
            btnXacThuc.textContent = 'Xác thực';
        }
    });

    // --- Đổi email: quay lại bước 1, reset mọi state OTP ---
    btnDoiEmail.addEventListener('click', () => {
        AnThongBao();
        ChuyenBuoc(1);
        inputEmail.value = emailHienTai;
        inputEmail.focus();
        // Dừng timer đếm ngược
        if (timerId) { clearInterval(timerId); timerId = null; }
        // Reset state OTP
        emailHienTai = '';
        inputOtp.value = '';
        demNguocEl.textContent = '0';
        // Enable nút gửi lại nếu đang bị disable
        btnGuiLai.disabled = false;
    });

    // --- Gửi lại OTP: gọi lại request endpoint, tôn trọng cooldown backend ---
    btnGuiLai.addEventListener('click', async () => {
        if (btnGuiLai.disabled) return;
        AnThongBao();
        btnGuiLai.disabled = true;

        try {
            await GuiOtpAsync(emailHienTai);
            BatDauDemNguoc(60);
            HienThongBao('ok', 'Đã gửi lại mã OTP mới.');
        } catch (err) {
            HienThongBao('error', err.message || 'Không thể gửi lại OTP.');
            btnGuiLai.disabled = false;
        }
    });
});
