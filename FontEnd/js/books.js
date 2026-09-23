// ==========================================================================
// Quản lý Sách (Admin) & Tìm kiếm nâng cao
// API Endpoints:
//   GET    /api/Book/search?keyword=&theloai=&tacgia=&namxuatban=&page=&pageSize=
//   GET    /api/Book/{id}        → chi tiết
//   POST   /api/Book             → thêm mới
//   PUT    /api/Book/{id}        → cập nhật
//   DELETE /api/Book/{id}        → xoá (khi chưa có bản sao)
// ==========================================================================

// ID sách đang chỉnh sửa (null = chế độ thêm mới)
let _editBookId = null;
let _searchDebounce = null;
const HANG_SACH = 10;   // 10 hàng/trang
let _trangSach = 1;

document.addEventListener('DOMContentLoaded', () => {
    // 1. Nếu ở trang Admin/Books
    const bookTableBody = document.getElementById('bookTableBody');
    if (bookTableBody) {
        loadBooks();
        loadFilterOptions(); // nạp dropdown Thể loại / Năm XB

        // Gắn sự kiện submit form thêm/sửa sách
        const bookForm = document.getElementById('bookForm');
        if (bookForm) {
            bookForm.addEventListener('submit', handleSaveBook);
        }
    }
});

// Gọi khi bấm nút số trang
function diTrangSach(tr) {
    _trangSach = tr;
    loadBooks();
}

// --- Tìm kiếm nâng cao: keyword (tên/ISBN/tác giả) + bộ lọc thể loại/tác giả/năm ---
function timkiemSach() {
    _trangSach = 1; // tìm mới → về trang 1
    // Tạo độ trễ 300ms tránh gọi API liên tục khi gõ
    clearTimeout(_searchDebounce);
    _searchDebounce = setTimeout(loadBooks, 300);
}

function locSach() {
    _trangSach = 1;
    loadBooks();
}

function datLaiBoLoc() {
    const search = document.getElementById('searchInput');
    const tacgia = document.getElementById('filterTacgia');
    const theloai = document.getElementById('filterTheloai');
    const nam = document.getElementById('filterNam');
    if (search) search.value = '';
    if (tacgia) tacgia.value = '';
    if (theloai) theloai.value = '';
    if (nam) nam.value = '';
    _trangSach = 1;
    loadBooks();
}

async function loadFilterOptions() {
    try {
        const res = await window.api.get('/Book/search?page=1&pageSize=1000');
        if (!res.success || !res.data) return;

        const dsTheLoai = [...new Set(res.data.map(b => b.theloai).filter(Boolean))].sort();
        const dsNam = [...new Set(res.data.map(b => b.namxuatban).filter(Boolean))].sort((a, b) => b - a);

        fillSelect('filterTheloai', dsTheLoai, 'Tất cả thể loại');
        fillSelect('filterNam', dsNam, 'Tất cả năm XB');
    } catch (err) {
        console.error('Lỗi nạp bộ lọc:', err);
    }
}

function fillSelect(id, values, labelDau) {
    const select = document.getElementById(id);
    if (!select) return;
    const hienTai = select.value;
    select.innerHTML = `<option value="">${labelDau}</option>`;
    values.forEach(v => {
        const opt = document.createElement('option');
        opt.value = v;
        opt.textContent = v;
        select.appendChild(opt);
    });
    // Giữ lại giá trị đang chọn (nếu vẫn còn)
    if ([...select.options].some(o => o.value === hienTai)) select.value = hienTai;
}

async function loadBooks() {
    try {
        const bookTableBody = document.getElementById('bookTableBody');
        bookTableBody.innerHTML = '<tr><td colspan="5" class="text-center">Đang tải dữ liệu...</td></tr>';

        // Gom tham số tìm kiếm từ UI
        const params = new URLSearchParams({ page: _trangSach, pageSize: HANG_SACH });
        const keyword = (document.getElementById('searchInput')?.value || '').trim();
        const theloai = document.getElementById('filterTheloai')?.value || '';
        const tacgia  = (document.getElementById('filterTacgia')?.value || '').trim();
        const nam     = document.getElementById('filterNam')?.value || '';
        if (keyword)          params.set('keyword', keyword);
        if (theloai)          params.set('theloai', theloai);
        if (tacgia)           params.set('tacgia', tacgia);
        if (nam !== '')       params.set('namxuatban', nam);

        // Gọi API backend
        const res = await window.api.get(`/Book/search?${params.toString()}`);
        const tong = res.totalItems ?? 0;

        // Trang hiện tại vượt tổng số trang (xoá hết ở trang cuối) → về trang 1
        if (res.success && (!res.data || res.data.length === 0) && _trangSach > 1) {
            _trangSach = 1;
            return loadBooks();
        }

        if (res.success && res.data && res.data.length > 0) {
            bookTableBody.innerHTML = '';
            res.data.forEach(book => {
                const statusBadge = book.sobancaosangio > 0
                    ? `<span class="badge badge-success">Còn ${book.sobancaosangio} quyển</span>`
                    : `<span class="badge badge-danger">Đã hết</span>`;

                const row = `
                    <tr>
                        <td style="font-weight: 500;">${book.title}</td>
                        <td class="text-muted">${book.isbn}</td>
                        <td>${book.tacgia || ''}</td>
                        <td>${statusBadge}</td>
                        <td>
                            <button class="btn btn-secondary" style="padding: 5px 10px; font-size: 0.8rem;" onclick="editBook('${book.book_id}')">Sửa</button>
                            <button class="btn btn-danger" style="padding: 5px 10px; font-size: 0.8rem;" onclick="deleteBook('${book.book_id}', \`${book.title.replace(/`/g, '')}\`)">Xóa</button>
                        </td>
                    </tr>
                `;
                bookTableBody.innerHTML += row;
            });
        } else {
            bookTableBody.innerHTML = '<tr><td colspan="5" class="text-center">Không tìm thấy sách nào</td></tr>';
        }
        renderphantrang('bookPagination', 'bookPageInfo', _trangSach, tong, HANG_SACH, 'diTrangSach');
    } catch (error) {
        console.error("Lỗi khi tải sách:", error);
        document.getElementById('bookTableBody').innerHTML = '<tr><td colspan="5" class="text-center" style="color: red;">Lỗi tải dữ liệu. Vui lòng thử lại.</td></tr>';
        renderphantrang('bookPagination', 'bookPageInfo', _trangSach, 0, HANG_SACH, 'diTrangSach');
    }
}

function openAddBookModal() {
    _editBookId = null;
    document.getElementById('bookForm').reset();
    document.getElementById('bookModalError').style.display = 'none';
    document.getElementById('bookModalTitle').textContent = 'Thêm Sách Mới';
    document.getElementById('bookModal').style.display = 'block';
}

function closeBookModal() {
    document.getElementById('bookModal').style.display = 'none';
}

async function handleSaveBook(e) {
    e.preventDefault();

    const errorDiv = document.getElementById('bookModalError');
    const btnSubmit = document.getElementById('btnSaveBook');
    errorDiv.style.display = 'none';

    const tongBanSaoRaw = document.getElementById('bookTotalCopies').value.trim();
    const bookPayload = {
        title: document.getElementById('bookTitle').value,
        isbn: document.getElementById('bookIsbn').value,
        tacgia: document.getElementById('bookAuthor').value,
        theloai: document.getElementById('bookCategory').value,
        nxb: document.getElementById('bookPublisher').value,
        namxuatban: document.getElementById('bookYear').value ? parseInt(document.getElementById('bookYear').value) : null,
        // Cho phép nhập 0 (chưa có bản sao); ô trống → mặc định 1
        tongsobancao: tongBanSaoRaw === '' ? 1 : parseInt(tongBanSaoRaw)
    };

    try {
        btnSubmit.innerHTML = 'Đang lưu...';
        btnSubmit.disabled = true;

        let res;
        if (_editBookId) {
            // Cập nhật: PUT /api/Book/{id}
            bookPayload.book_id = _editBookId;
            res = await window.api.put(`/Book/${_editBookId}`, bookPayload);
        } else {
            // Thêm mới: POST /api/Book
            res = await window.api.post('/Book', bookPayload);
        }

        if (res.success) {
            alert(_editBookId ? 'Cập nhật sách thành công!' : 'Thêm sách thành công!');
            closeBookModal();
            loadBooks();       // reload data
            loadFilterOptions(); // có sách mới → làm mới dropdown bộ lọc
        } else {
            throw new Error(res.message || "Lỗi khi lưu sách.");
        }
    } catch (error) {
        errorDiv.textContent = error.message;
        errorDiv.style.display = 'block';
    } finally {
        btnSubmit.innerHTML = 'Lưu Sách';
        btnSubmit.disabled = false;
    }
}

async function editBook(id) {
    try {
        const res = await window.api.get(`/Book/${id}`);
        if (!res.success || !res.data) throw new Error(res.message || 'Không tìm thấy sách.');

        const b = res.data;
        _editBookId = b.book_id;
        document.getElementById('bookTitle').value = b.title || '';
        document.getElementById('bookIsbn').value = b.isbn || '';
        document.getElementById('bookAuthor').value = b.tacgia || '';
        document.getElementById('bookCategory').value = b.theloai || '';
        document.getElementById('bookPublisher').value = b.nxb || '';
        document.getElementById('bookYear').value = b.namxuatban || '';
        document.getElementById('bookTotalCopies').value = b.tongsobancao ?? 1;

        document.getElementById('bookModalError').style.display = 'none';
        document.getElementById('bookModalTitle').textContent = 'Cập Nhật Sách';
        document.getElementById('bookModal').style.display = 'block';
    } catch (error) {
        alert(error.message);
    }
}

async function deleteBook(id, title) {
    if (!confirm(`Xác nhận xoá sách "${title}"?\n(Lưu ý: chỉ xoá được khi sách chưa có bản sao)`)) return;
    try {
        const res = await window.api.delete(`/Book/${id}`);
        if (res.success) {
            alert('Xoá sách thành công!');
            loadBooks();
            loadFilterOptions();
        } else {
            alert('Không thể xoá: ' + (res.message || 'lỗi không xác định'));
        }
    } catch (error) {
        alert('Không thể xoá: ' + error.message);
    }
}
