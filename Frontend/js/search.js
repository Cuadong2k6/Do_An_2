// ==========================================================================
// Tra Cứu Sách (Độc Giả) — tìm kiếm nâng cao + Giỏ hàng mượn sách
// API: GET  /api/Book/search?keyword=&theloai=&page=&pageSize=  (Anonymous)
//      GET  /api/Reader/{id}                                     → lấy somughin
//      POST /api/Loan/mine   { book_ids[], due_date }              → tạo phiếu từ giỏ
// ==========================================================================

// Số ngày mặc định cho mỗi lần mượn
const GIO_HANG_SO_NGAY_MAC_DINH = 14;

// Giỏ hàng chỉ tồn tại trong trang hiện tại (đóng trang là mất, không lưu localStorage)
// Mỗi dòng là 1 đầu sách, có thể mượn nhiều bản sao → có số lượng
let _gioHang = [];        // [{ book_id, title, tacgia, soLuong, tonKho }]
let _gioHangToiDa = 5;    // = read.somughin, nạp từ API
let _soDangMuon = 0;      // số cuốn đang mượn, nạp từ API (tính cả phiếu quá hạn)
let _ketQuaSach = [];     // kết quả tìm kiếm gần nhất, dùng để tra tên khi thêm vào giỏ

document.addEventListener('DOMContentLoaded', () => {
    const searchForm = document.getElementById('searchForm');
    if (searchForm) {
        searchForm.addEventListener('submit', e => {
            e.preventDefault();
            traCuuSach();
        });
        napdsTheLoai();
        traCuuSach(); // hiển thị toàn bộ sách khi vào trang

        // Chỉ bạn đọc mới được tự mượn từ giỏ hàng
        if (LaBanDoc()) {
            napsoihanGioHang();
            document.getElementById('btnGioHang').style.display = 'inline-block';
        }
    }
});

// ====================== GIỎ HÀNG ======================

function LaBanDoc() {
    const role = (JSON.parse(localStorage.getItem('user') || '{}').role || '').toLowerCase();
    return role === 'bandoc' || role === 'reader';
}

// Lấy giới hạn mượn (somughin) và số cuốn đang mượn của chính bạn đọc đang đăng nhập
async function napsoihanGioHang() {
    const readerId = JSON.parse(localStorage.getItem('user') || '{}').id;
    if (!readerId) return;

    try {
        const res = await window.api.get(`/Reader/${readerId}`);
        if (res.success && res.data && res.data.somughin > 0) {
            _gioHangToiDa = res.data.somughin;
        }
    } catch (err) {
        // Không lấy được thì dùng giá trị mặc định, server vẫn kiểm tra lại khi tạo phiếu
        console.error('Lỗi lấy giới hạn mượn:', err);
    }

    // Số cuốn đang mượn: đếm trong lịch sử mượn các phiếu chưa trả
    try {
        const res = await window.api.get(`/Loan/history/${readerId}?page=1&pageSize=500`);
        if (res.success && Array.isArray(res.data)) {
            _soDangMuon = res.data.filter(x => !x.return_date).length;
        }
    } catch (err) {
        // Không lấy được thì coi như đang mượn 0, server vẫn kiểm tra lại khi tạo phiếu
        console.error('Lỗi lấy số sách đang mượn:', err);
    }
}

// Tổng số cuốn trong giỏ (cộng dồn số lượng của từng đầu sách)
function TongSoCuonGio() {
    return _gioHang.reduce((tong, x) => tong + x.soLuong, 0);
}

// Thẻ chỉ cho mượn tối đa somughin cuốn tính cả sách đang mượn → giỏ chỉ được thêm phần còn dư
function SoCuonConDuocMuan() {
    return Math.max(0, _gioHangToiDa - _soDangMuon);
}

function NgayYYYYMMDD(d) {
    const yyyy = d.getFullYear();
    const mm   = String(d.getMonth() + 1).padStart(2, '0');
    const dd   = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
}

// Số cuốn tối đa có thể thêm cho 1 đầu sách đang có trong giỏ:
// không vượt tồn kho bản sao rảnh và không vượt phần cuốn còn được mượn của thẻ.
function GioiHanSoLuong(x) {
    return Math.min(x.tonKho, SoCuonConDuocMuan());
}

function VeGioHang() {
    const box = document.getElementById('gioHangDanhSach');
    if (!box) return;

    const tongCuon = TongSoCuonGio();
    document.getElementById('gioHangCount').textContent = tongCuon;

    if (_gioHang.length === 0) {
        box.innerHTML = '<p class="text-muted">Giỏ hàng trống. Bấm <strong>Thêm vào giỏ</strong> ở từng cuốn sách.</p>';
    } else {
        box.innerHTML =
            '<table style="width:100%;border-collapse:collapse;">' +
                '<thead><tr>' +
                    '<th style="text-align:left;padding:8px;border-bottom:1px solid var(--border-color)">Sách</th>' +
                    '<th style="text-align:center;padding:8px;border-bottom:1px solid var(--border-color);width:110px;">Số lượng</th>' +
                    '<th style="text-align:right;padding:8px;border-bottom:1px solid var(--border-color)">Thao tác</th>' +
                '</tr></thead><tbody>' +
                _gioHang.map(x => {
                    const toiDa = GioiHanSoLuong(x);
                    const hetQuyen = x.soLuong >= toiDa;
                    return `
                    <tr>
                        <td style="padding:8px;border-bottom:1px solid var(--border-color)">
                            <strong>${EscapeHtml(x.title)}</strong><br>
                            <span class="text-muted" style="font-size:0.8rem;">
                                ${EscapeHtml(x.tacgia) || 'Không rõ tác giả'} · còn ${x.tonKho} bản sao
                            </span>
                        </td>
                        <td style="padding:8px;border-bottom:1px solid var(--border-color);text-align:center;">
                            <button type="button" class="btn btn-secondary btn-sm"
                                    style="width:30px;padding:2px;"
                                    ${x.soLuong <= 1 ? 'disabled' : ''}
                                    onclick="GiamSoLuongGio('${EscapeHtml(x.book_id)}')">
                                <i class="fa-solid fa-minus"></i>
                            </button>
                            <span style="display:inline-block;min-width:26px;font-weight:bold;">${x.soLuong}</span>
                            <button type="button" class="btn btn-secondary btn-sm"
                                    style="width:30px;padding:2px;"
                                    ${hetQuyen ? 'disabled' : ''}
                                    onclick="TangSoLuongGio('${EscapeHtml(x.book_id)}')">
                                <i class="fa-solid fa-plus"></i>
                            </button>
                        </td>
                        <td style="padding:8px;border-bottom:1px solid var(--border-color);text-align:right;">
                            <button type="button" class="btn btn-secondary btn-sm"
                                    onclick="XoaKhoiGioHang('${EscapeHtml(x.book_id)}')">
                                <i class="fa-solid fa-trash"></i> Bỏ
                            </button>
                        </td>
                    </tr>`;
                }).join('') +
                '</tbody></table>' +
                `<p class="text-muted mt-1" style="font-size:0.85rem;">
                    Tổng <strong>${tongCuon}</strong> cuốn — thẻ cho phép mượn ${_gioHangToiDa} cuốn,
                    bạn đang mượn ${_soDangMuon} cuốn nên còn được thêm
                    <strong>${SoCuonConDuocMuan()}</strong> cuốn.
                    Tất cả gộp thành <strong>1 phiếu mượn</strong>.
                </p>`;
    }

    const btn = document.getElementById('btnThanhToanGio');
    btn.innerHTML = tongCuon === 0 ? 'Mượn sách trong giỏ' : `Mượn ${tongCuon} cuốn`;
    btn.disabled = tongCuon === 0;
}

function ThemVaoGioHang(bookId) {
    if (_gioHang.some(x => x.book_id === bookId)) return;

    if (TongSoCuonGio() >= SoCuonConDuocMuan()) {
        // Nút đã bị disable khi giỏ đầy, đây chỉ là lưới an toàn → dùng alert cho chắc chắn thấy
        alert(SoCuonConDuocMuan() <= 0
            ? `Thẻ của bạn đang mượn ${_soDangMuon}/${_gioHangToiDa} cuốn, đã đạt giới hạn. Hãy trả sách trước.`
            : `Bạn đang mượn ${_soDangMuon}/${_gioHangToiDa} cuốn, chỉ mượn thêm tối đa ${SoCuonConDuocMuan()} cuốn.`);
        return;
    }

    const sach = _ketQuaSach.find(x => x.book_id === bookId);
    if (!sach) return;

    _gioHang.push({ book_id: bookId, title: sach.title, tacgia: sach.tacgia, soLuong: 1, tonKho: sach.sobancaosangio });
    VeGioHang();
    traCuuSach(); // vẽ lại để cập nhật trạng thái nút của từng sách
}

// Tăng số lượng 1 cuốn, chặn theo tồn kho và phần cuốn còn được mượn của thẻ
function TangSoLuongGio(bookId) {
    const x = _gioHang.find(i => i.book_id === bookId);
    if (!x) return;

    const toiDa = GioiHanSoLuong(x);
    if (x.soLuong >= toiDa) {
        HienThongBaoGioHang(
            x.tonKho <= SoCuonConDuocMuan()
                ? `Chỉ còn ${x.tonKho} bản sao rảnh của sách này.`
                : `Thẻ của bạn chỉ còn được mượn thêm ${SoCuonConDuocMuan()} cuốn.`, true);
        return;
    }

    x.soLuong++;
    AnThongBaoGioHang();
    VeGioHang();
}

function GiamSoLuongGio(bookId) {
    const x = _gioHang.find(i => i.book_id === bookId);
    if (!x || x.soLuong <= 1) return;

    x.soLuong--;
    AnThongBaoGioHang();
    VeGioHang();
}

function XoaKhoiGioHang(bookId) {
    _gioHang = _gioHang.filter(x => x.book_id !== bookId);
    AnThongBaoGioHang();
    VeGioHang();
    traCuuSach();
}

function MoModalGioHang() {
    const ngayMacDinh = new Date();
    ngayMacDinh.setDate(ngayMacDinh.getDate() + GIO_HANG_SO_NGAY_MAC_DINH);

    const input = document.getElementById('gioHangNgayTra');
    input.value = NgayYYYYMMDD(ngayMacDinh);
    input.min   = NgayYYYYMMDD(new Date());

    // Giữ con số 14 trong HTML đồng bộ với hằng số JS
    document.getElementById('gioHangSoNgayMacDinh').textContent = GIO_HANG_SO_NGAY_MAC_DINH;

    AnThongBaoGioHang();
    VeGioHang();
    document.getElementById('gioHangModal').style.display = 'block';
}

function DongModalGioHang() {
    document.getElementById('gioHangModal').style.display = 'none';
}

function HienThongBaoGioHang(message, laLoi) {
    const box = document.getElementById('gioHangThongBao');
    box.textContent = EscapeHtml(message);
    box.style.display = 'block';
    box.style.background = laLoi ? '#fee2e2' : '#dcfce7';
    box.style.color = laLoi ? '#991b1b' : '#166534';
}

function AnThongBaoGioHang() {
    document.getElementById('gioHangThongBao').style.display = 'none';
}

async function ThanhToanGioHang() {
    const tongCuon = TongSoCuonGio();
    if (tongCuon === 0) return;

    const ngayTra = document.getElementById('gioHangNgayTra').value;
    if (!ngayTra) {
        HienThongBaoGioHang('Vui lòng chọn ngày trả sách.', true);
        return;
    }

    const homNay = new Date();
    homNay.setHours(0, 0, 0, 0);
    if (new Date(ngayTra) < homNay) {
        HienThongBaoGioHang('Ngày trả sách phải từ hôm nay trở đi.', true);
        return;
    }

    const btn   = document.getElementById('btnThanhToanGio');
    const textGoc = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = 'Đang tạo phiếu...';

    // Mỗi đầu sách gửi lên số lần bằng số lượng trong giỏ
    // (cùng book_id lặp lại = hệ thống gán các bản sao khác nhau)
    const dsBookId = [];
    _gioHang.forEach(x => { for (let i = 0; i < x.soLuong; i++) dsBookId.push(x.book_id); });

    try {
        const res = await window.api.post('/Loan/mine', {
            book_ids: dsBookId,
            due_date: new Date(ngayTra).toISOString()
        });

        _gioHang = [];
        HienThongBaoGioHang(res.message || `Mượn thành công ${tongCuon} cuốn.`, false);
        VeGioHang();
        traCuuSach(); // số bản còn trống đã thay đổi
    } catch (err) {
        // api.js ném lỗi khi HTTP != 2xx, err.message là thông báo từ server
        HienThongBaoGioHang(err.message || 'Không tạo được phiếu mượn.', true);
        btn.disabled = false;
        btn.innerHTML = textGoc;
    }
}

// ====================== TRA CỨU SÁCH ======================

// Nạp dropdown Thể loại từ dữ liệu sách
async function napdsTheLoai() {
    try {
        const res = await window.api.get('/Book/search?page=1&pageSize=1000');
        if (!res.success || !res.data) return;
        const select = document.getElementById('searchTheloai');
        if (!select) return;

        [...new Set(res.data.map(b => b.theloai).filter(Boolean))]
            .sort()
            .forEach(tl => {
                const opt = document.createElement('option');
                opt.value = tl;
                opt.textContent = tl;
                select.appendChild(opt);
            });
    } catch (err) {
        console.error('Lỗi nạp thể loại:', err);
    }
}

// Nút thêm vào giỏ — trạng thái phụ thuộc tồn kho, giỏ hàng và phần cuốn còn được mượn của thẻ
function NutThemVaoGio(sach) {
    const trongGio = _gioHang.some(x => x.book_id === sach.book_id);

    if (trongGio) {
        return '<button class="btn btn-secondary btn-block btn-sm" disabled><i class="fa-solid fa-check"></i> Đã trong giỏ</button>';
    }
    if (sach.sobancaosangio <= 0) {
        return '<button class="btn btn-secondary btn-block btn-sm" disabled>Đã hết bản sao</button>';
    }
    if (TongSoCuonGio() >= SoCuonConDuocMuan()) {
        return `<button class="btn btn-secondary btn-block btn-sm" disabled>Đã đủ ${SoCuonConDuocMuan()} cuốn</button>`;
    }
    return `<button class="btn btn-primary btn-block btn-sm"
                    onclick="ThemVaoGioHang('${EscapeHtml(sach.book_id)}')">
                <i class="fa-solid fa-cart-plus"></i> Thêm vào giỏ
            </button>`;
}

// Tìm kiếm: keyword (tên / ISBN / tác giả) + bộ lọc thể loại
async function traCuuSach() {
    const box = document.getElementById('ketQuaTimKiem');
    if (!box) return;
    box.innerHTML = '<p class="text-muted">Đang tìm...</p>';

    const params = new URLSearchParams({ page: 1, pageSize: 50 });
    const keyword = (document.getElementById('searchInput')?.value || '').trim();
    const theloai = document.getElementById('searchTheloai')?.value || '';
    if (keyword) params.set('keyword', keyword);
    if (theloai) params.set('theloai', theloai);

    const hienThiGio = LaBanDoc();

    try {
        const res = await window.api.get(`/Book/search?${params.toString()}`);

        if (res.success && res.data && res.data.length > 0) {
            _ketQuaSach = res.data;
            box.innerHTML = '<div class="d-flex" style="gap:20px;flex-wrap:wrap;">' +
                res.data.map(b => {
                    const badge = b.sobancaosangio > 0
                        ? `<span class="badge badge-success">Còn ${b.sobancaosangio} quyển</span>`
                        : `<span class="badge badge-danger">Đã hết</span>`;
                    return `
                    <div style="border:1px solid var(--border-color);border-radius:8px;padding:15px;width:220px;text-align:center;">
                        <div style="height:150px;background:#e2e8f0;border-radius:6px;margin-bottom:15px;display:flex;align-items:center;justify-content:center;">
                            <i class="fa-solid fa-book" style="font-size:3rem;color:#94a3b8;"></i>
                        </div>
                        <h4 style="margin-bottom:5px;">${EscapeHtml(b.title)}</h4>
                        <p class="text-muted" style="font-size:0.85rem;margin-bottom:5px;">${EscapeHtml(b.tacgia) || 'Không rõ tác giả'}</p>
                        <p class="text-muted" style="font-size:0.8rem;margin-bottom:8px;">${EscapeHtml(b.theloai)}${b.namxuatban ? ' · ' + EscapeHtml(b.namxuatban) : ''}</p>
                        ${badge}
                        ${hienThiGio ? `<div style="margin-top:10px;">${NutThemVaoGio(b)}</div>` : ''}
                    </div>`;
                }).join('') + '</div>' +
                `<p class="text-muted mt-2" style="font-size:0.85rem;">Tìm thấy ${res.totalItems ?? res.data.length} kết quả.</p>`;
        } else {
            _ketQuaSach = [];
            box.innerHTML = '<p class="text-muted">Không tìm thấy sách nào phù hợp.</p>';
        }
    } catch (err) {
        box.innerHTML = `<p style="color:red">Lỗi tìm kiếm: ${EscapeHtml(err.message)}</p>`;
    }
}