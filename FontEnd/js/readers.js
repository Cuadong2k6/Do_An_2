// ==========================================================================
// Quản lý Độc Giả (Admin)
// API Endpoints:
//   GET    /api/Reader?keyword=&page=&pageSize=   → { success, data: [...ReaderModel] }
//   GET    /api/Reader/{id}                       → { success, data: ReaderModel }
//   POST   /api/Reader/register                   → { success, data: reader_id }
//   PUT    /api/Reader/{id}                       → { success }
//   DELETE /api/Reader/{id}                       → { success }
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('readerTableBody')) {
        loadReaders();

        const readerForm = document.getElementById('readerForm');
        if (readerForm) {
            readerForm.addEventListener('submit', handleSaveReader);
        }
    }
});

function toISODate(d) {
    return d.toISOString().split('T')[0];
}

const HANG_DOC_GIA = 10;   // 10 hàng/trang
let _trangDocGia = 1;

// Gọi khi người dùng gõ ô tìm kiếm / đổi bộ lọc → về trang 1
function timkiemDocGia() {
    _trangDocGia = 1;
    loadReaders();
}

// Gọi khi bấm nút số trang
function diTrangDocGia(tr) {
    _trangDocGia = tr;
    loadReaders();
}

async function loadReaders() {
    const tbody = document.getElementById('readerTableBody');
    tbody.innerHTML = '<tr><td colspan="7" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    try {
        // Đọc từ ô tìm kiếm + dropdown trạng thái trên header
        const keyword = (document.getElementById('searchInput')?.value || '').trim();
        const trangthai = document.getElementById('filterTrangthai')?.value ?? '';

        const params = new URLSearchParams({ page: _trangDocGia, pageSize: HANG_DOC_GIA });
        if (keyword !== '') params.set('keyword', keyword);
        if (trangthai !== '') params.set('trangthai', trangthai);

        const res = await window.api.get(`/Reader?${params.toString()}`);
        const tong = res.totalItems ?? 0;

        // Trang hiện tại vượt tổng số trang (xoá hết ở trang cuối) → về trang 1
        if (res.success && (!res.data || res.data.length === 0) && _trangDocGia > 1) {
            _trangDocGia = 1;
            return loadReaders();
        }

        if (res.success && res.data && res.data.length > 0) {
            tbody.innerHTML = '';
            res.data.forEach(r => {
                const trangthaiMap = { 0: 'badge-success', 1: 'badge-warning', 2: 'badge-danger' };
                const trangthaiText = { 0: 'Hoạt động', 1: 'Hết hạn', 2: 'Bị khoá' };
                const badgeClass = trangthaiMap[r.trangthai] || 'badge-secondary';
                const badgeText  = trangthaiText[r.trangthai] || 'Không rõ';

                const ngayhethan = r.ngayhethan ? new Date(r.ngayhethan).toLocaleDateString('vi-VN') : '—';

                tbody.innerHTML += `
                    <tr>
                        <td style="font-weight:500">${r.hoten}</td>
                        <td><span class="badge badge-secondary">${r.so_the || '—'}</span></td>
                        <td class="text-muted">${r.email}</td>
                        <td>${r.sodienthoai || '—'}</td>
                        <td>${ngayhethan}</td>
                        <td><span class="badge ${badgeClass}">${badgeText}</span></td>
                        <td>
                            <button class="btn btn-secondary" style="padding:5px 10px;font-size:0.8rem"
                                onclick="moModalSuaReader('${r.reader_id}')">Sửa</button>
                            <button class="btn btn-danger" style="padding:5px 10px;font-size:0.8rem"
                                onclick="xoadocgia('${r.reader_id}', \`${r.hoten}\`)">Xoá</button>
                        </td>
                    </tr>`;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="7" style="text-align:center">Không tìm thấy độc giả nào</td></tr>';
        }
        renderphantrang('readerPagination', 'readerPageInfo', _trangDocGia, tong, HANG_DOC_GIA, 'diTrangDocGia');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:red">Lỗi tải dữ liệu: ${err.message}</td></tr>`;
        renderphantrang('readerPagination', 'readerPageInfo', _trangDocGia, 0, HANG_DOC_GIA, 'diTrangDocGia');
    }
}

// Biến lưu trạng thái modal (thêm mới hay sửa)
let _editReaderId = null;
let _editReaderTrangthai = 0;

function moModalThemReader() {
    _editReaderId = null;
    _editReaderTrangthai = 0;
    document.getElementById('readerForm').reset();
    document.getElementById('readerNgayHetHan').value = toISODate(new Date(new Date().setFullYear(new Date().getFullYear() + 1)));
    document.getElementById('readerSoMuon').value = 3;
    document.getElementById('readerEmail').disabled = false;
    document.getElementById('readerSoThe').disabled = false;
    document.getElementById('readerModalError').style.display = 'none';
    document.getElementById('readerModalTitle').textContent = 'Thêm Độc Giả Mới';
    document.getElementById('fieldMatKhau').style.display = 'block'; // chỉ hiện khi thêm mới
    document.getElementById('readerModal').style.display = 'block';
}

async function moModalSuaReader(id) {
    try {
        const res = await window.api.get(`/Reader/${id}`);
        if (!res.success || !res.data) throw new Error(res.message || 'Không tìm thấy độc giả.');
        const r = res.data;

        _editReaderId = id;
        _editReaderTrangthai = r.trangthai;

        document.getElementById('readerHoten').value = r.hoten || '';
        document.getElementById('readerEmail').value = r.email || '';
        document.getElementById('readerSoThe').value = r.so_the || '';
        document.getElementById('readerNgayHetHan').value = r.ngayhethan ? r.ngayhethan.split('T')[0] : '';
        document.getElementById('readerSoMuon').value = r.somughin || 3;
        document.getElementById('readerSdt').value = r.sodienthoai || '';
        document.getElementById('readerDiachi').value = r.diachi || '';

        // Khi sửa: email + số thẻ không thay đổi (backend không cho đổi)
        document.getElementById('readerEmail').disabled = true;
        document.getElementById('readerSoThe').disabled = true;

        document.getElementById('readerModalError').style.display = 'none';
        document.getElementById('readerModalTitle').textContent = 'Cập Nhật Độc Giả';
        document.getElementById('fieldMatKhau').style.display = 'none'; // ẩn khi sửa
        document.getElementById('readerModal').style.display = 'block';
    } catch (err) {
        alert(err.message);
    }
}

function dongModalReader() {
    document.getElementById('readerModal').style.display = 'none';
}

async function handleSaveReader(e) {
    e.preventDefault();
    const errorDiv = document.getElementById('readerModalError');
    const btnSubmit = document.getElementById('btnSaveReader');
    errorDiv.style.display = 'none';
    btnSubmit.disabled = true;
    btnSubmit.innerHTML = 'Đang lưu...';

    const ngayHetHan = document.getElementById('readerNgayHetHan').value;

    const payload = {
        hoten:        document.getElementById('readerHoten').value,
        email:        document.getElementById('readerEmail').value,
        so_the:       document.getElementById('readerSoThe').value,
        sodienthoai:  document.getElementById('readerSdt').value,
        diachi:       document.getElementById('readerDiachi').value,
        matkhau:      document.getElementById('readerMatkhau')?.value || '',
        ngayhethan:   ngayHetHan ? new Date(ngayHetHan).toISOString()
                                 : new Date(new Date().setFullYear(new Date().getFullYear() + 1)).toISOString(),
        somughin:     parseInt(document.getElementById('readerSoMuon').value) || 3,
        trangthai:    _editReaderTrangthai
    };

    try {
        let res;
        if (_editReaderId) {
            // PUT /api/Reader/{id}
            res = await window.api.put(`/Reader/${_editReaderId}`, payload);
        } else {
            // POST /api/Reader/register
            res = await window.api.post('/Reader/register', payload);
        }

        if (res.success) {
            alert(_editReaderId ? 'Cập nhật thành công!' : 'Thêm độc giả thành công!');
            dongModalReader();
            loadReaders();
        } else {
            throw new Error(res.message || 'Lỗi khi lưu dữ liệu.');
        }
    } catch (err) {
        errorDiv.textContent = err.message;
        errorDiv.style.display = 'block';
    } finally {
        btnSubmit.disabled = false;
        btnSubmit.innerHTML = 'Lưu';
    }
}

async function xoadocgia(id, hoten) {
    if (!confirm(`Xác nhận xoá độc giả "${hoten}"?\n(Lưu ý: chỉ xoá được khi độc giả chưa có lịch sử mượn sách)`)) return;
    try {
        const res = await window.api.delete(`/Reader/${id}`);
        if (res.success) {
            alert('Xoá độc giả thành công!');
            loadReaders();
        } else {
            throw new Error(res.message || 'Xoá thất bại.');
        }
    } catch (err) {
        alert('Không thể xoá: ' + err.message);
    }
}
