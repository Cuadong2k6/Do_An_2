// Quản lý tài khoản admin.
// - Độc giả: xem, sửa thông tin cơ bản và khoá/mở khoá (tự đăng ký qua Google OAuth / Gmail OTP).
// - Thủ thư (Staff): CRUD đầy đủ. API contract theo StaffService/StaffDtos.
//   Staff API hỗ trợ phân trang; tìm kiếm được lọc phía client.
const ACCOUNT_PAGE_SIZE = 10;
let accountPage = 1;
let editingStaff = null;
let _readerDetailId = null; // Lưu reader_id đang xem/sửa chi tiết

document.addEventListener('DOMContentLoaded', () => {
    if (!document.getElementById('accountTableBody')) return;
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    if ((user.role || '').toLowerCase() !== 'admin') {
        window.location.replace('../../index.html');
        return;
    }
    const staffForm = document.getElementById('staffForm');
    if (staffForm) staffForm.addEventListener('submit', LuuStaff);
    const readerForm = document.getElementById('readerForm');
    if (readerForm) readerForm.addEventListener('submit', LuuDocGia);
    TaiTaiKhoan();
});

// ==================== TẢI DANH SÁCH ====================
async function TaiTaiKhoan() {
    const body = document.getElementById('accountTableBody');
    const type = document.getElementById('accountType').value;
    // Cả 2 tab dùng chung 5 cột: Họ tên | Email | Tài khoản/Số thẻ | Trạng thái | Thao tác
    const colCount = 5;
    // Nút thêm chỉ hiện khi đang xem tab Thủ thư
    const btnThem = document.getElementById('btnThemStaff');
    if (btnThem) btnThem.style.display = type === 'staff' ? '' : 'none';
    const searchBox = document.getElementById('accountSearch');
    if (searchBox) searchBox.style.display = type === 'confirmation' ? 'none' : '';
    body.innerHTML = `<tr><td colspan="${colCount}" style="text-align:center">Đang tải...</td></tr>`;
    if (type === 'confirmation') {
        await TaiHangChoXacNhanTaiKhoan(body, colCount);
        return;
    }
    try {
        const params = new URLSearchParams({ page: accountPage, pageSize: ACCOUNT_PAGE_SIZE });
        const keyword = document.getElementById('accountSearch').value.trim().toLocaleLowerCase();
        const res = type === 'reader'
            ? await window.api.get(`/Reader?${params}${keyword ? `&keyword=${encodeURIComponent(keyword)}` : ''}`)
            : await window.api.get(`/Staff?${params}`);
        if (!res.success) throw new Error(res.message || 'Không tải được danh sách.');
        const rows = (res.data || []).filter(item => type !== 'staff' || !keyword ||
            [item.hoten, item.email, item.taikhoan].some(value => String(value || '').toLocaleLowerCase().includes(keyword)));
        if (!rows.length) {
            body.innerHTML = `<tr><td colspan="${colCount}" style="text-align:center">Không tìm thấy tài khoản</td></tr>`;
        } else if (type === 'reader') {
            // Tab độc giả: bỏ nút khoá trực tiếp, chỉ có nút Sửa → mở form chi tiết
            body.innerHTML = rows.map(item => {
                const locked = Number(item.trangthai) === 2;
                const statusMap = { 0: 'Hoạt động', 1: 'Hết hạn', 2: 'Bị khoá' };
                const statusText = statusMap[item.trangthai] || 'Không rõ';
                const badgeClass = locked ? 'badge-danger' : 'badge-success';
                return `<tr>
                    <td>${EscapeHtml(item.hoten)}</td>
                    <td>${EscapeHtml(item.email)}</td>
                    <td>${EscapeHtml(item.so_the || '—')}</td>
                    <td><span class="badge ${badgeClass}">${EscapeHtml(statusText)}</span></td>
                    <td><button class="btn btn-secondary" onclick="MoChiTietDocGia('${EscapeHtml(item.reader_id)}')">Sửa</button></td>
                </tr>`;
            }).join('');
        } else {
            // Tab thủ thư: chỉ nút Sửa → mở modal; khoá/mở khoá nằm trong modal (giống độc giả)
            body.innerHTML = rows.map(item => {
                const locked = Boolean(item.is_locked);
                const badgeClass = locked ? 'badge-danger' : 'badge-success';
                return `<tr>
                    <td>${EscapeHtml(item.hoten)}</td>
                    <td>${EscapeHtml(item.email)}</td>
                    <td>${EscapeHtml(item.taikhoan)}</td>
                    <td><span class="badge ${badgeClass}">${locked ? 'Bị khoá' : 'Hoạt động'}</span></td>
                    <td><button class="btn btn-secondary" onclick="SuaThuThu('${EscapeHtml(item.user_id)}')">Sửa</button></td>
                </tr>`;
            }).join('');
        }
        RenderPhanTrang('accountPagination', 'accountPageInfo', accountPage, res.totalItems || 0, ACCOUNT_PAGE_SIZE, 'DiTrangTaiKhoan');
    } catch (error) {
        body.innerHTML = `<tr><td colspan="${colCount}" style="text-align:center;color:red">${EscapeHtml(error.message)}</td></tr>`;
    }
}

function DiTrangTaiKhoan(page) { accountPage = page; TaiTaiKhoan(); }

// Đổi tab Độc giả / Thủ thư: quay về trang 1 để tránh đứng ở trang không tồn tại
function DoiLoaiTaiKhoan() { accountPage = 1; TaiTaiKhoan(); }

// Hàng chờ kích hoạt. Tên riêng vì readers.js không load cùng trang này.
async function TaiHangChoXacNhanTaiKhoan(body, colCount) {
    const pageInfo = document.getElementById('accountPageInfo');
    const pagination = document.getElementById('accountPagination');
    if (pageInfo) pageInfo.textContent = '';
    if (pagination) pagination.innerHTML = '';
    try {
        const res = await window.api.get('/Reader/activations/pending');
        if (!res.success) throw new Error(res.message || 'Không tải được danh sách chờ duyệt.');
        const rows = res.data || [];
        if (!rows.length) {
            body.innerHTML = `<tr><td colspan="${colCount}" style="text-align:center">Không có yêu cầu kích hoạt chờ duyệt</td></tr>`;
            return;
        }
        body.innerHTML = rows.map(item => {
            const id = EscapeHtml(item.reader_id);
            const hoten = EscapeHtml(item.hoten || '');
            return `<tr>
                <td>${hoten}</td>
                <td>${EscapeHtml(item.email || '')}</td>
                <td>${EscapeHtml(item.so_the || '—')}</td>
                <td><span class="badge badge-warning">Chờ duyệt</span></td>
                <td>
                    <button class="btn btn-primary" style="padding:5px 10px;font-size:0.8rem" onclick="DuyetXacNhanTaiKhoan('${id}')">Duyệt</button>
                    <button class="btn btn-danger" style="padding:5px 10px;font-size:0.8rem" onclick="TuChoiXacNhanTaiKhoan('${id}')">Từ chối</button>
                </td>
            </tr>`;
        }).join('');
    } catch (error) {
        body.innerHTML = `<tr><td colspan="${colCount}" style="text-align:center;color:red">${EscapeHtml(error.message)}</td></tr>`;
    }
}

async function DuyetXacNhanTaiKhoan(id) {
    try {
        const res = await window.api.put(`/Reader/activations/${id}/approve`, {});
        if (!res.success) throw new Error(res.message || 'Không duyệt được yêu cầu.');
        alert(res.message || 'Đã duyệt yêu cầu kích hoạt.');
        await TaiTaiKhoan();
    } catch (error) {
        alert(error.message || 'Không duyệt được yêu cầu kích hoạt.');
    }
}

async function TuChoiXacNhanTaiKhoan(id) {
    if (!confirm('Từ chối yêu cầu kích hoạt này? Tài khoản sẽ bị khoá.')) return;
    try {
        const res = await window.api.put(`/Reader/activations/${id}/reject`, {});
        if (!res.success) throw new Error(res.message || 'Không từ chối được yêu cầu.');
        alert(res.message || 'Đã từ chối yêu cầu kích hoạt.');
        await TaiTaiKhoan();
    } catch (error) {
        alert(error.message || 'Không từ chối được yêu cầu kích hoạt.');
    }
}

// ==================== STAFF CRUD ====================
function MoFormStaff(staff = null) {
    editingStaff = staff;
    document.getElementById('staffModalTitle').textContent = staff ? 'Cập nhật thủ thư' : 'Thêm thủ thư';
    document.getElementById('staffId').value = staff ? staff.user_id : '';
    document.getElementById('staffHoTen').value = staff ? staff.hoten : '';
    document.getElementById('staffEmail').value = staff ? staff.email : '';
    document.getElementById('staffTenDangNhap').value = staff ? staff.taikhoan : '';
    // Tài khoản không được sửa sau khi tạo
    document.getElementById('staffTenDangNhap').readOnly = Boolean(staff);

    // Số điện thoại chỉ hiện khi sửa (không hiện ở danh sách và khi thêm mới)
    const sdtLabel = document.getElementById('lblStaffSdt');
    const sdtField = document.getElementById('staffSdt');
    sdtField.value = staff ? (staff.sodienthoai || '') : '';
    sdtLabel.style.display = staff ? '' : 'none';

    const pwField = document.getElementById('staffMatKhau');
    // KHÔNG hiển thị mật khẩu cũ; luôn để trống
    pwField.value = '';
    // Bắt buộc nhập mật khẩu khi tạo mới, tuỳ chọn khi sửa
    pwField.required = !staff;
    document.getElementById('staffPwRequired').style.display = staff ? 'none' : '';

    // Nút khoá/mở khoá chỉ có khi sửa tài khoản đã tồn tại
    const btnKhoa = document.getElementById('btnKhoaMoStaff');
    if (staff) {
        const locked = Boolean(staff.is_locked);
        btnKhoa.textContent = locked ? 'Mở khoá' : 'Khoá';
        btnKhoa.className = locked ? 'btn btn-primary' : 'btn btn-danger';
        btnKhoa.style.display = '';
    } else {
        btnKhoa.style.display = 'none';
    }

    document.getElementById('staffModal').style.display = 'block';
}

function DongFormStaff() {
    document.getElementById('staffModal').style.display = 'none';
    editingStaff = null;
}

async function SuaThuThu(id) {
    try {
        const res = await window.api.get(`/Staff/${encodeURIComponent(id)}`);
        if (!res.success) throw new Error(res.message || 'Không tải được thông tin.');
        MoFormStaff(res.data);
    } catch (e) { alert(e.message); }
}

async function LuuStaff(event) {
    event.preventDefault();
    const id = document.getElementById('staffId').value;
    const payload = {
        hoten: document.getElementById('staffHoTen').value.trim(),
        email: document.getElementById('staffEmail').value.trim(),
        taikhoan: document.getElementById('staffTenDangNhap').value.trim()
    };
    const sodienthoai = document.getElementById('staffSdt').value.trim();
    const password = document.getElementById('staffMatKhau').value;
    // Chỉ gửi matkhau khi có nhập (tạo mới bắt buộc, sửa thì tuỳ)
    if (password) payload.matkhau = password;

    try {
        if (id) {
            // Cập nhật — chuỗi rỗng = xoá số điện thoại
            payload.sodienthoai = sodienthoai;
            await window.api.put(`/Staff/${encodeURIComponent(id)}`, payload);
        } else {
            // Tạo mới — không gửi số điện thoại rỗng
            if (sodienthoai) payload.sodienthoai = sodienthoai;
            if (!payload.matkhau) throw new Error('Mật khẩu là bắt buộc khi tạo tài khoản.');
            await window.api.post('/Staff', payload);
        }
        DongFormStaff();
        TaiTaiKhoan();
    } catch (error) { alert(error.message); }
}

async function KhoaMoThuThu() {
    if (!editingStaff) return;
    const locked = Boolean(editingStaff.is_locked);
    if (!confirm(`Bạn muốn ${locked ? 'mở khoá' : 'khoá'} tài khoản này?`)) return;
    try {
        await window.api.post(`/Staff/${encodeURIComponent(editingStaff.user_id)}/${locked ? 'unlock' : 'lock'}`);
        DongFormStaff();
        TaiTaiKhoan();
    } catch (error) { alert(error.message); }
}

// ==================== READER — XEM, SỬA & KHOÁ/MỞ ====================
// Admin không tạo độc giả; chỉ xem, sửa thông tin cơ bản và khoá/mở khoá.
// Độc giả tự đăng ký qua Google OAuth hoặc Gmail OTP.
// Nút Sửa trên bảng mở modal, trong modal có nút khoá/mở và lưu thay đổi.
async function MoChiTietDocGia(id) {
    try {
        const res = await window.api.get(`/Reader/${encodeURIComponent(id)}`);
        if (!res.success) throw new Error(res.message || 'Không tải được thông tin độc giả.');
        const d = res.data;
        _readerDetailId = id;
        const locked = Number(d.trangthai) === 2;

        // Gán giá trị vào form (input/textarea dùng .value)
        document.getElementById('readerHoTen').value = d.hoten || '';
        document.getElementById('readerEmail').value = d.email || '';
        document.getElementById('readerSoThe').value = d.so_the || '';
        document.getElementById('readerNgayCap').value = d.ngaycap ? d.ngaycap.substring(0, 10) : '';
        document.getElementById('readerTrangThai').value = String(Number(d.trangthai));
        document.getElementById('readerSoMuGhin').value = d.somughin != null ? d.somughin : '';
        document.getElementById('readerSoDienThoai').value = d.sodienthoai || '';
        document.getElementById('readerDiaChi').value = d.diachi || '';
        document.getElementById('readerNgayHetHan').value = d.ngayhethan ? d.ngayhethan.substring(0, 10) : '';

        const btnKhoa = document.getElementById('btnKhoaMoReader');
        if (btnKhoa) {
            btnKhoa.textContent = locked ? 'Mở khoá' : 'Khoá';
            btnKhoa.className = locked ? 'btn btn-primary' : 'btn btn-danger';
        }
        document.getElementById('readerModal').style.display = 'block';
    } catch (e) { alert(e.message); }
}

function DongFormReader() {
    document.getElementById('readerModal').style.display = 'none';
    _readerDetailId = null;
}

async function KhoaMoReader() {
    if (!_readerDetailId) return;
    const btn = document.getElementById('btnKhoaMoReader');
    const isLocking = btn && btn.textContent.trim() === 'Khoá';
    if (!confirm(`Bạn muốn ${isLocking ? 'khoá' : 'mở khoá'} tài khoản độc giả này?`)) return;
    try {
        await window.api.put(`/Reader/${encodeURIComponent(_readerDetailId)}/${isLocking ? 'lock' : 'unlock'}`, null);
        DongFormReader();
        TaiTaiKhoan();
    } catch (error) { alert(error.message); }
}

// Lưu thay đổi thông tin độc giả (chỉ các field editable)
async function LuuDocGia(e) {
    e.preventDefault();
    if (!_readerDetailId) return;
    const ngaycapRaw = document.getElementById('readerNgayCap').value;
    const ngayhethanRaw = document.getElementById('readerNgayHetHan').value;
    const somughinRaw = document.getElementById('readerSoMuGhin').value;
    const payload = {
        hoten: document.getElementById('readerHoTen').value.trim(),
        sodienthoai: document.getElementById('readerSoDienThoai').value.trim() || null,
        diachi: document.getElementById('readerDiaChi').value.trim() || null,
        ngaycap: ngaycapRaw ? ngaycapRaw + 'T00:00:00' : null,
        trangthai: Number(document.getElementById('readerTrangThai').value),
        ngayhethan: ngayhethanRaw ? ngayhethanRaw + 'T00:00:00' : null,
        somughin: Number(somughinRaw)
    };
    if (!payload.hoten) {
        alert('Họ tên là bắt buộc.');
        return;
    }
    // SP/BLL: somughin null hoặc <= 0 bị từ chối; cột NOT NULL. Trống không được gửi thành 0.
    if (somughinRaw === '' || !Number.isInteger(payload.somughin) || payload.somughin <= 0) {
        alert('Số sách mượn tối đa phải lớn hơn 0.');
        return;
    }
    try {
        const res = await window.api.put(`/Reader/${encodeURIComponent(_readerDetailId)}`, payload);
        if (!res.success) throw new Error(res.message || 'Không cập nhật được độc giả.');
        DongFormReader();
        TaiTaiKhoan();
    } catch (error) { alert(error.message); }
}
