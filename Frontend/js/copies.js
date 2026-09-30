// ==========================================================================
// Quản Lý Bản Sao Sách (Admin)
// API Endpoints:
//   GET  /api/Copy?book_id=&status=&page=&pageSize=   → danh sách bản sao
//   GET  /api/Copy/{id}                               → chi tiết
//   POST /api/Copy                                    → thêm mới
//   PUT  /api/Copy/{id}                               → cập nhật (shelf_id, status)
//   DELETE /api/Copy/{id}                             → xoá (khi chưa mượn)
//   GET  /api/Book/search?page=1&pageSize=1000        -> lấy danh sách đầu sách cho dropdown
//   GET  /api/Shelf?page=1&pageSize=1000              -> lấy danh sách kệ cho dropdown
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('copyTableBody')) {
        TaiBanSao();
        TaiTuyChon(); // nạp dropdown đầu sách & kệ
        
        const copyForm = document.getElementById('copyForm');
        if (copyForm) {
            copyForm.addEventListener('submit', XuLyLuuBanSao);
        }
    }
});

let _editCopyId = null;
let _trangCopy = 1;
const HANG_COPY = 10;

// Tìm kiếm
function TimkiemBanSao() {
    _trangCopy = 1;
    TaiBanSao();
}

// Bộ lọc
function LocBanSao() {
    _trangCopy = 1;
    TaiBanSao();
}

function DatLaiLocBanSao() {
    document.getElementById('copySearchInput').value = '';
    document.getElementById('filterCopyBook').value = '';
    document.getElementById('filterCopyStatus').value = '';
    document.getElementById('filterCopyShelf').value = '';
    _trangCopy = 1;
    TaiBanSao();
}

// Gọi khi bấm nút số trang
function DiTrangBanSao(tr) {
    _trangCopy = tr;
    TaiBanSao();
}

// Nạp dropdown đầu sách & kệ cho filter & modal
async function TaiTuyChon() {
    try {
        // Lấy danh sách đầu sách
        const booksRes = await window.api.get('/Book/search?page=1&pageSize=1000');
        if (booksRes.success && booksRes.data) {
            NapChon('filterCopyBook', booksRes.data.map(b => ({value: b.book_id, label: `${b.title} (${b.isbn})`})), 'Tất cả đầu sách');
            NapChon('copyBookId', booksRes.data.map(b => ({value: b.book_id, label: `${b.title} (${b.isbn})`})), 'Chọn đầu sách');
        }
        
        // Lấy danh sách kệ
        const shelfRes = await window.api.get('/Shelf?page=1&pageSize=1000');
        if (shelfRes.success && shelfRes.data) {
            NapChon('filterCopyShelf', shelfRes.data.map(s => ({value: s.shelf_id, label: s.location_code})), 'Tất cả kệ');
            NapChon('copyShelfId', shelfRes.data.map(s => ({value: s.shelf_id, label: s.location_code})), 'Chưa đặt kệ');
        }
    } catch (err) {
        console.error('Lỗi nạp bộ lọc bản sao:', err);
    }
}

function NapChon(id, items, labelDau) {
    const select = document.getElementById(id);
    if (!select) return;
    const hienTai = select.value;
    select.innerHTML = `<option value="">${labelDau}</option>`;
    items.forEach(item => {
        const opt = document.createElement('option');
        opt.value = item.value;
        opt.textContent = item.label;
        select.appendChild(opt);
    });
    if ([...select.options].some(o => o.value === hienTai)) select.value = hienTai;
}

// Load danh sách bản sao
async function TaiBanSao() {
    const tbody = document.getElementById('copyTableBody');
    tbody.innerHTML = '<tr><td colspan="7" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    
    try {
        const keyword = (document.getElementById('copySearchInput')?.value || '').trim();
        const bookId = document.getElementById('filterCopyBook')?.value || '';
        const status = document.getElementById('filterCopyStatus')?.value || '';
        const shelfId = document.getElementById('filterCopyShelf')?.value || '';
        
        const params = new URLSearchParams({ page: _trangCopy, pageSize: HANG_COPY });
        if (bookId) params.set('book_id', bookId);
        if (status !== '') params.set('status', status);
        if (shelfId) params.set('shelf_id', shelfId);
        
        // API Copy/search không có keyword, lọc client-side nếu có keyword
        const res = await window.api.get(`/Copy?${params.toString()}`);
        let data = res.data || [];
        const tong = res.totalItems ?? 0;
        
        // Lọc keyword client-side (mã bản sao, tên sách, ISBN)
        if (keyword) {
            const kw = keyword.toLowerCase();
            data = data.filter(c => 
                (c.mabancao && c.mabancao.toLowerCase().includes(kw)) ||
                (c.book_title && c.book_title.toLowerCase().includes(kw)) ||
                (c.isbn && c.isbn.toLowerCase().includes(kw))
            );
        }
        
        if (data.length === 0 && _trangCopy > 1) {
            _trangCopy = 1;
            return TaiBanSao();
        }
        
        if (data.length > 0) {
            tbody.innerHTML = '';
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            const isAdminRole = (user.role || '').toLowerCase() === 'admin';
            
            data.forEach(c => {
                const ngaynhap = c.ngaynhap ? new Date(c.ngaynhap).toLocaleDateString('vi-VN') : '—';
                const shelfLoc = c.shelf_location || 'Chưa đặt';
                
                const statusMap = { 0: 'badge-success', 1: 'badge-warning', 2: 'badge-danger' };
                const statusText = { 0: 'Sẵn có', 1: 'Đã mượn', 2: 'Hỏng' };
                const badgeClass = statusMap[c.status] || 'badge-secondary';
                const badgeText  = statusText[c.status] || 'Không rõ';
                
                // Chỉ Admin mới thấy nút Xoá
                const deleteBtn = isAdminRole
                    ? `<button class="btn btn-danger" style="padding:5px 10px;font-size:0.8rem;margin-left:5px" onclick="XoaBanSao('${c.copy_id}', \`${c.mabancao}\`)">Xóa</button>`
                    : '';
                
                tbody.innerHTML += `
                    <tr>
                        <td><span class="badge badge-secondary">${c.mabancao || '—'}</span></td>
                        <td>${c.book_title || '—'}<br><small class="text-muted">ISBN: ${c.isbn || ''}</small></td>
                        <td class="text-muted">${c.isbn || '—'}</td>
                        <td>${shelfLoc}</td>
                        <td><span class="badge ${badgeClass}">${badgeText}</span></td>
                        <td>${ngaynhap}</td>
                        <td>
                            <button class="btn btn-secondary" style="padding:5px 10px;font-size:0.8rem" onclick="SuaBanSao('${c.copy_id}')">Sửa</button>
                            ${deleteBtn}
                        </td>
                    </tr>`;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="7" style="text-align:center">Không tìm thấy bản sao nào</td></tr>';
        }
        
        // Dùng tong từ API (chưa lọc keyword) cho phân trang
        RenderPhanTrang('copyPagination', 'copyPageInfo', _trangCopy, tong, HANG_COPY, 'DiTrangBanSao');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
        RenderPhanTrang('copyPagination', 'copyPageInfo', _trangCopy, 0, HANG_COPY, 'DiTrangBanSao');
    }
}

// Modal
function moModalThemBanSao() {
    _editCopyId = null;
    document.getElementById('copyForm').reset();
    document.getElementById('copyModalError').style.display = 'none';
    document.getElementById('copyModalTitle').textContent = 'Thêm Bản Sao Mới';
    document.getElementById('copyModal').style.display = 'block';
}

function dongModalCopy() {
    document.getElementById('copyModal').style.display = 'none';
}

async function SuaBanSao(id) {
    try {
        const res = await window.api.get(`/Copy/${id}`);
        if (!res.success || !res.data) throw new Error(res.message || 'Không tìm thấy bản sao.');
        
        const c = res.data;
        _editCopyId = id;
        
        document.getElementById('copyBookId').value = c.book_id || '';
        document.getElementById('copyMaBanSao').value = c.mabancao || '';
        document.getElementById('copyShelfId').value = c.shelf_id || '';
        document.getElementById('copyStatus').value = c.status !== undefined ? c.status : 0;
        
        // Khi sửa: mã bản sao & đầu sách không đổi (backend check unique)
        document.getElementById('copyBookId').disabled = true;
        document.getElementById('copyMaBanSao').disabled = true;
        
        document.getElementById('copyModalError').style.display = 'none';
        document.getElementById('copyModalTitle').textContent = 'Cập Nhật Bản Sao';
        document.getElementById('copyModal').style.display = 'block';
    } catch (err) {
        alert(err.message);
    }
}

async function XuLyLuuBanSao(e) {
    e.preventDefault();
    const errorDiv = document.getElementById('copyModalError');
    const btnSubmit = document.getElementById('btnSaveCopy');
    errorDiv.style.display = 'none';
    btnSubmit.disabled = true;
    btnSubmit.innerHTML = 'Đang lưu...';
    
    const payload = {
        book_id: document.getElementById('copyBookId').value,
        mabancao: document.getElementById('copyMaBanSao').value,
        shelf_id: document.getElementById('copyShelfId').value || null,
        status: parseInt(document.getElementById('copyStatus').value) || 0
    };
    
    try {
        let res;
        if (_editCopyId) {
            // PUT /api/Copy/{id} - chỉ cho phép cập nhật shelf_id, status
            res = await window.api.put(`/Copy/${_editCopyId}`, {
                shelf_id: payload.shelf_id,
                status: payload.status
            });
        } else {
            // POST /api/Copy
            res = await window.api.post('/Copy', payload);
        }
        
        if (res.success) {
            alert(_editCopyId ? 'Cập nhật bản sao thành công!' : 'Thêm bản sao thành công!');
            dongModalCopy();
            TaiBanSao();
        } else {
            throw new Error(res.message || 'Lỗi khi lưu bản sao.');
        }
    } catch (error) {
        errorDiv.textContent = error.message;
        errorDiv.style.display = 'block';
    } finally {
        btnSubmit.disabled = false;
        btnSubmit.innerHTML = 'Lưu';
    }
}

async function XoaBanSao(id, maBanSao) {
    if (!confirm(`Xác nhận xoá bản sao "${maBanSao}"?\n(Lưu ý: chỉ xoá được khi bản sao chưa từng được mượn)`)) return;
    try {
        const res = await window.api.delete(`/Copy/${id}`);
        if (res.success) {
            alert('Xoá bản sao thành công!');
            TaiBanSao();
        } else {
            alert('Không thể xoá: ' + (res.message || 'lỗi không xác định'));
        }
    } catch (error) {
        alert('Không thể xoá: ' + error.message);
    }
}
