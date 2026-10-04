// ==========================================================================
// Quản Lý Kệ Sách (Admin)
// API Endpoints:
//   GET  /api/Shelf?keyword=&page=&pageSize=   → danh sách kệ
//   GET  /api/Shelf/{id}                       → chi tiết
//   POST /api/Shelf                            → thêm mới
//   PUT  /api/Shelf/{id}                       → cập nhật
//   DELETE /api/Shelf/{id}                     → xoá (khi chưa có bản sao)
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('shelfTableBody')) {
        TaiKeSach();
        
        const shelfForm = document.getElementById('shelfForm');
        if (shelfForm) {
            shelfForm.addEventListener('submit', XuLyLuuKeSach);
        }
    }
});

let _editShelfId = null;
let _trangShelf = 1;
const HANG_SHELF = 10;

// Tìm kiếm
function TimkiemKeSach() {
    _trangShelf = 1;
    TaiKeSach();
}

// Gọi khi bấm nút số trang
function DiTrangKeSach(tr) {
    _trangShelf = tr;
    TaiKeSach();
}

// Load danh sách kệ
async function TaiKeSach() {
    const tbody = document.getElementById('shelfTableBody');
    tbody.innerHTML = '<tr><td colspan="5" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    
    try {
        const keyword = (document.getElementById('shelfSearchInput')?.value || '').trim();
        
        const params = new URLSearchParams({ page: _trangShelf, pageSize: HANG_SHELF });
        if (keyword) params.set('keyword', keyword);
        
        const res = await window.api.get(`/Shelf?${params.toString()}`);
        const tong = res.totalItems ?? 0;
        
        if (res.success && (!res.data || res.data.length === 0) && _trangShelf > 1) {
            _trangShelf = 1;
            return TaiKeSach();
        }
        
        if (res.success && res.data && res.data.length > 0) {
            tbody.innerHTML = '';
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            const isAdminRole = (user.role || '').toLowerCase() === 'admin';
            
            res.data.forEach(s => {
                // Chỉ Admin mới thấy nút Xoá
                const deleteBtn = isAdminRole
                    ? `<button class="btn btn-danger" style="padding:5px 10px;font-size:0.8rem;margin-left:5px" onclick="XoaKeSach(${EscapeHtml(s.shelf_id)}, \`${EscapeHtml(s.location_code).replace(/`/g, '&#96;')}\`)">Xóa</button>`
                    : '';
                
                tbody.innerHTML += `
                    <tr>
                        <td><span class="badge badge-secondary">${EscapeHtml(s.shelf_id)}</span></td>
                        <td style="font-weight:500">${EscapeHtml(s.location_code)}</td>
                        <td>${EscapeHtml(s.mota) || '—'}</td>
                        <td style="text-align:center;font-weight:bold">${EscapeHtml(s.sobancao) || 0}</td>
                        <td>
                            <button class="btn btn-secondary" style="padding:5px 10px;font-size:0.8rem" onclick="editShelf(${EscapeHtml(s.shelf_id)})">Sửa</button>
                            ${deleteBtn}
                        </td>
                    </tr>`;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="5" style="text-align:center">Không tìm thấy kệ sách nào</td></tr>';
        }
        
        RenderPhanTrang('shelfPagination', 'shelfPageInfo', _trangShelf, tong, HANG_SHELF, 'DiTrangKeSach');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align:center;color:red">Lỗi: ${EscapeHtml(err.message)}</td></tr>`;
        RenderPhanTrang('shelfPagination', 'shelfPageInfo', _trangShelf, 0, HANG_SHELF, 'DiTrangKeSach');
    }
}

// Modal
function moModalThemKe() {
    _editShelfId = null;
    document.getElementById('shelfForm').reset();
    document.getElementById('shelfModalError').style.display = 'none';
    document.getElementById('shelfModalTitle').textContent = 'Thêm Kệ Sách Mới';
    document.getElementById('shelfModal').style.display = 'block';
}

function DongModalKeSach() {
    document.getElementById('shelfModal').style.display = 'none';
}

async function editShelf(id) {
    try {
        const res = await window.api.get(`/Shelf/${id}`);
        if (!res.success || !res.data) throw new Error(res.message || 'Không tìm thấy kệ sách.');
        
        const s = res.data;
        _editShelfId = id;
        
        document.getElementById('shelfLocationCode').value = s.location_code || '';
        document.getElementById('shelfMota').value = s.mota || '';
        
        document.getElementById('shelfModalError').style.display = 'none';
        document.getElementById('shelfModalTitle').textContent = 'Cập Nhật Kệ Sách';
        document.getElementById('shelfModal').style.display = 'block';
    } catch (err) {
        alert(err.message);
    }
}

async function XuLyLuuKeSach(e) {
    e.preventDefault();
    const errorDiv = document.getElementById('shelfModalError');
    const btnSubmit = document.getElementById('btnSaveShelf');
    errorDiv.style.display = 'none';
    btnSubmit.disabled = true;
    btnSubmit.innerHTML = 'Đang lưu...';
    
    const payload = {
        location_code: document.getElementById('shelfLocationCode').value,
        mota: document.getElementById('shelfMota').value
    };
    
    try {
        let res;
        if (_editShelfId) {
            // PUT /api/Shelf/{id}
            res = await window.api.put(`/Shelf/${_editShelfId}`, payload);
        } else {
            // POST /api/Shelf
            res = await window.api.post('/Shelf', payload);
        }
        
        if (res.success) {
            alert(_editShelfId ? 'Cập nhật kệ sách thành công!' : 'Thêm kệ sách thành công!');
            DongModalKeSach();
            TaiKeSach();
        } else {
            throw new Error(res.message || 'Lỗi khi lưu kệ sách.');
        }
    } catch (error) {
        errorDiv.textContent = error.message;
        errorDiv.style.display = 'block';
    } finally {
        btnSubmit.disabled = false;
        btnSubmit.innerHTML = 'Lưu';
    }
}

async function XoaKeSach(id, locationCode) {
    if (!confirm(`Xác nhận xoá kệ "${locationCode}"?\n(Lưu ý: chỉ xoá được khi kệ chưa chứa bản sao nào)`)) return;
    try {
        const res = await window.api.delete(`/Shelf/${id}`);
        if (res.success) {
            alert('Xoá kệ sách thành công!');
            TaiKeSach();
        } else {
            alert('Không thể xoá: ' + (res.message || 'lỗi không xác định'));
        }
    } catch (error) {
        alert('Không thể xoá: ' + error.message);
    }
}
