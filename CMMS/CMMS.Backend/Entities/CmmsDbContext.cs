using Microsoft.EntityFrameworkCore;

namespace CMMS.Backend.Entities;

public class CmmsDbContext(DbContextOptions<CmmsDbContext> options) : DbContext(options)
{
    public DbSet<VAI_TRO> VAI_TRO => Set<VAI_TRO>();
    public DbSet<VAI_TRO_QUYEN> VAI_TRO_QUYEN => Set<VAI_TRO_QUYEN>();
    public DbSet<THU_VIEN_BIEU_MAU> THU_VIEN_BIEU_MAU => Set<THU_VIEN_BIEU_MAU>();
    public DbSet<PHAN_XUONG_BO_PHAN> PHAN_XUONG_BO_PHAN => Set<PHAN_XUONG_BO_PHAN>();
    public DbSet<NHAN_VIEN> NHAN_VIEN => Set<NHAN_VIEN>();
    public DbSet<TAI_KHOAN> TAI_KHOAN => Set<TAI_KHOAN>();
    public DbSet<NHAT_KY_HE_THONG> NHAT_KY_HE_THONG => Set<NHAT_KY_HE_THONG>();

    public DbSet<NHOM_THIET_BI> NHOM_THIET_BI => Set<NHOM_THIET_BI>();
    public DbSet<DANH_MUC_MA_LOI> DANH_MUC_MA_LOI => Set<DANH_MUC_MA_LOI>();
    public DbSet<THIET_BI> THIET_BI => Set<THIET_BI>();
    public DbSet<THONG_SO_THIET_BI> THONG_SO_THIET_BI => Set<THONG_SO_THIET_BI>();

    public DbSet<CAU_HINH_MUC_UU_TIEN> CAU_HINH_MUC_UU_TIEN => Set<CAU_HINH_MUC_UU_TIEN>();
    public DbSet<YEU_CAU_BAO_TRI> YEU_CAU_BAO_TRI => Set<YEU_CAU_BAO_TRI>();

    public DbSet<MAU_CHECKLIST> MAU_CHECKLIST => Set<MAU_CHECKLIST>();
    public DbSet<HANG_MUC_CHECKLIST> HANG_MUC_CHECKLIST => Set<HANG_MUC_CHECKLIST>();
    public DbSet<KE_HOACH_BAO_TRI> KE_HOACH_BAO_TRI => Set<KE_HOACH_BAO_TRI>();

    public DbSet<LENH_BAO_TRI> LENH_BAO_TRI => Set<LENH_BAO_TRI>();
    public DbSet<CHUYEN_MON_KTV> CHUYEN_MON_KTV => Set<CHUYEN_MON_KTV>();
    public DbSet<PHAN_CONG_KTV> PHAN_CONG_KTV => Set<PHAN_CONG_KTV>();
    public DbSet<KET_QUA_CHECKLIST> KET_QUA_CHECKLIST => Set<KET_QUA_CHECKLIST>();
    public DbSet<BAO_CAO_SU_CO> BAO_CAO_SU_CO => Set<BAO_CAO_SU_CO>();

    public DbSet<PHU_TUNG> PHU_TUNG => Set<PHU_TUNG>();
    public DbSet<DINH_MUC_LINH_KIEN> DINH_MUC_LINH_KIEN => Set<DINH_MUC_LINH_KIEN>();
    public DbSet<PHIEU_CAP_NHAT_TON_KHO> PHIEU_CAP_NHAT_TON_KHO => Set<PHIEU_CAP_NHAT_TON_KHO>();
    public DbSet<CHI_TIET_CAP_NHAT_TON_KHO> CHI_TIET_CAP_NHAT_TON_KHO => Set<CHI_TIET_CAP_NHAT_TON_KHO>();
    public DbSet<PHIEU_DU_TRU_XUAT_DUNG> PHIEU_DU_TRU_XUAT_DUNG => Set<PHIEU_DU_TRU_XUAT_DUNG>();
    public DbSet<CHI_TIET_PHIEU_XUAT> CHI_TIET_PHIEU_XUAT => Set<CHI_TIET_PHIEU_XUAT>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. VAI_TRO
        modelBuilder.Entity<VAI_TRO>(e =>
        {
            e.ToTable("VAI_TRO");
            e.HasKey(x => x.VaiTroId);
            e.Property(x => x.VaiTroId).ValueGeneratedNever();
            e.Property(x => x.TenVaiTro).HasMaxLength(50).IsRequired();
            e.Property(x => x.MoTa).HasMaxLength(255);
            e.HasIndex(x => x.TenVaiTro).IsUnique();
        });

        // 2. VAI_TRO_QUYEN
        modelBuilder.Entity<VAI_TRO_QUYEN>(e =>
        {
            e.ToTable("VAI_TRO_QUYEN");
            e.HasKey(x => new { x.VaiTroId, x.MaQuyen });
            e.Property(x => x.MaQuyen).HasMaxLength(50);
            e.HasOne(x => x.VaiTro)
                .WithMany(v => v.VAI_TRO_QUYEN)
                .HasForeignKey(x => x.VaiTroId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. THU_VIEN_BIEU_MAU
        modelBuilder.Entity<THU_VIEN_BIEU_MAU>(e =>
        {
            e.ToTable("THU_VIEN_BIEU_MAU");
            e.HasKey(x => x.BieuMauId);
            e.Property(x => x.BieuMauId).HasMaxLength(30);
            e.Property(x => x.TenBieuMau).HasMaxLength(150).IsRequired();
            e.Property(x => x.LoaiBieuMau).HasMaxLength(50).IsRequired();
            e.Property(x => x.DinhDangTep).HasMaxLength(10).IsRequired();
            e.Property(x => x.DuongDanTep).HasMaxLength(255).IsRequired();
            e.Property(x => x.MoTa).HasMaxLength(255);
        });

        // 4. PHAN_XUONG_BO_PHAN
        modelBuilder.Entity<PHAN_XUONG_BO_PHAN>(e =>
        {
            e.ToTable("PHAN_XUONG_BO_PHAN");
            e.HasKey(x => x.DonViId);
            e.Property(x => x.DonViId).HasMaxLength(30);
            e.Property(x => x.TenDonVi).HasMaxLength(100).IsRequired();
            e.Property(x => x.DonViChaId).HasMaxLength(30);
            e.Property(x => x.LoaiDonVi).HasMaxLength(30).IsRequired();
            e.Property(x => x.NguoiPhuTrachId).HasMaxLength(20);
            e.Property(x => x.MoTa).HasMaxLength(255);

            e.HasOne(x => x.DonViCha)
                .WithMany(d => d.DonViCon)
                .HasForeignKey(x => x.DonViChaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.NguoiPhuTrach)
                .WithMany()
                .HasForeignKey(x => x.NguoiPhuTrachId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 5. NHAN_VIEN
        modelBuilder.Entity<NHAN_VIEN>(e =>
        {
            e.ToTable("NHAN_VIEN");
            e.HasKey(x => x.NhanVienId);
            e.Property(x => x.NhanVienId).HasMaxLength(20);
            e.Property(x => x.HoTen).HasMaxLength(100).IsRequired();
            e.Property(x => x.SoDienThoai).HasMaxLength(15).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.DonViId).HasMaxLength(30).IsRequired();
            e.Property(x => x.ChucVu).HasMaxLength(50).IsRequired();
            e.Property(x => x.ChuyenNganh).HasMaxLength(50);
            e.Property(x => x.BacTho).HasMaxLength(20);
            e.Property(x => x.CaLamViec).HasMaxLength(20);
            e.Property(x => x.TrangThaiSanSang).HasMaxLength(20);
            e.Property(x => x.TrangThai).HasMaxLength(20).HasDefaultValue("DangLamViec").IsRequired();

            e.HasOne(x => x.DonVi)
                .WithMany(d => d.NhanViens)
                .HasForeignKey(x => x.DonViId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 6. TAI_KHOAN
        modelBuilder.Entity<TAI_KHOAN>(e =>
        {
            e.ToTable("TAI_KHOAN");
            e.HasKey(x => x.TaiKhoanId);
            e.Property(x => x.NhanVienId).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.NhanVienId).IsUnique();
            e.Property(x => x.TenDangNhap).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.TenDangNhap).IsUnique();
            e.Property(x => x.MatKhauHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.TrangThaiTaiKhoan).HasMaxLength(20).HasDefaultValue("ChoDuyet").IsRequired();
            e.Property(x => x.NguoiDuyetId).HasMaxLength(20);

            e.HasOne(x => x.NhanVien)
                .WithOne(n => n.TaiKhoan)
                .HasForeignKey<TAI_KHOAN>(x => x.NhanVienId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.VaiTro)
                .WithMany(v => v.TAI_KHOAN)
                .HasForeignKey(x => x.VaiTroId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.NguoiDuyet)
                .WithMany()
                .HasForeignKey(x => x.NguoiDuyetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 7. NHAT_KY_HE_THONG
        modelBuilder.Entity<NHAT_KY_HE_THONG>(e =>
        {
            e.ToTable("NHAT_KY_HE_THONG");
            e.HasKey(x => x.NhatKyId);
            e.Property(x => x.LoaiThaoTac).HasMaxLength(50).IsRequired();
            e.Property(x => x.DoiTuongTacDong).HasMaxLength(100);
            e.Property(x => x.MoTaChiTiet).IsRequired();
            e.HasIndex(x => x.ThoiDiem);

            e.HasOne(x => x.TaiKhoan)
                .WithMany(t => t.NhatKys)
                .HasForeignKey(x => x.TaiKhoanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 8. NHOM_THIET_BI
        modelBuilder.Entity<NHOM_THIET_BI>(e =>
        {
            e.ToTable("NHOM_THIET_BI");
            e.HasKey(x => x.NhomThietBiId);
            e.Property(x => x.NhomThietBiId).HasMaxLength(30);
            e.Property(x => x.TenNhom).HasMaxLength(100).IsRequired();
            e.Property(x => x.MoTa).HasMaxLength(255);
        });

        // 9. DANH_MUC_MA_LOI
        modelBuilder.Entity<DANH_MUC_MA_LOI>(e =>
        {
            e.ToTable("DANH_MUC_MA_LOI");
            e.HasKey(x => x.MaLoiId);
            e.Property(x => x.MaLoiId).HasMaxLength(20);
            e.Property(x => x.TenLoaiSuCo).HasMaxLength(100).IsRequired();
            e.Property(x => x.MoTaDauHieu).HasMaxLength(255).IsRequired();
        });

        // 10. THIET_BI
        modelBuilder.Entity<THIET_BI>(e =>
        {
            e.ToTable("THIET_BI");
            e.HasKey(x => x.ThietBiId);
            e.Property(x => x.ThietBiId).HasMaxLength(30);
            e.Property(x => x.TenThietBi).HasMaxLength(150).IsRequired();
            e.Property(x => x.ThietBiChaId).HasMaxLength(30);
            e.Property(x => x.NhomThietBiId).HasMaxLength(30).IsRequired();
            e.Property(x => x.ViTriLapDatId).HasMaxLength(30).IsRequired();
            e.Property(x => x.BoPhanQuanLyId).HasMaxLength(30).IsRequired();
            e.Property(x => x.Model).HasMaxLength(50).IsRequired();
            e.Property(x => x.Serial).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Serial).IsUnique();
            e.Property(x => x.NhaSanXuat).HasMaxLength(100).IsRequired();
            e.Property(x => x.TrangThaiVanHanh).HasMaxLength(30).HasDefaultValue("DangHoatDong").IsRequired();
            e.Property(x => x.DuongDanAnh).HasMaxLength(255);
            e.Property(x => x.DuongDanTaiLieu).HasMaxLength(255);

            e.HasOne(x => x.ThietBiCha)
                .WithMany(t => t.CumCon)
                .HasForeignKey(x => x.ThietBiChaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.NhomThietBi)
                .WithMany(n => n.ThietBis)
                .HasForeignKey(x => x.NhomThietBiId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ViTriLapDat)
                .WithMany(d => d.ThietBiTaiViTri)
                .HasForeignKey(x => x.ViTriLapDatId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.BoPhanQuanLy)
                .WithMany(d => d.ThietBiQuanLy)
                .HasForeignKey(x => x.BoPhanQuanLyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. THONG_SO_THIET_BI
        modelBuilder.Entity<THONG_SO_THIET_BI>(e =>
        {
            e.ToTable("THONG_SO_THIET_BI");
            e.HasKey(x => x.ThongSoId);
            e.Property(x => x.ThietBiId).HasMaxLength(30).IsRequired();
            e.Property(x => x.TenThongSo).HasMaxLength(100).IsRequired();
            e.Property(x => x.GiaTri).HasMaxLength(100).IsRequired();
            e.Property(x => x.DonViDo).HasMaxLength(30);
            e.Property(x => x.MoTa).HasMaxLength(255);

            e.HasOne(x => x.ThietBi)
                .WithMany(t => t.ThongSos)
                .HasForeignKey(x => x.ThietBiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 12. CAU_HINH_MUC_UU_TIEN
        modelBuilder.Entity<CAU_HINH_MUC_UU_TIEN>(e =>
        {
            e.ToTable("CAU_HINH_MUC_UU_TIEN");
            e.HasKey(x => x.MucUuTienId);
            e.Property(x => x.MucUuTienId).ValueGeneratedNever();
            e.Property(x => x.TenMucUuTien).HasMaxLength(30).IsRequired();
            e.Property(x => x.MaMauHienThi).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.TenMucUuTien).IsUnique();
            e.HasIndex(x => x.ThuTuUuTien).IsUnique();
        });

        // 13. YEU_CAU_BAO_TRI
        modelBuilder.Entity<YEU_CAU_BAO_TRI>(e =>
        {
            e.ToTable("YEU_CAU_BAO_TRI");
            e.HasKey(x => x.YeuCauId);
            e.Property(x => x.YeuCauId).HasMaxLength(30);
            e.Property(x => x.ThietBiId).HasMaxLength(30).IsRequired();
            e.Property(x => x.NguoiBaoId).HasMaxLength(20).IsRequired();
            e.Property(x => x.DuongDanAnhSuCo).HasMaxLength(255);
            e.Property(x => x.TrangThaiYeuCau).HasMaxLength(30).HasDefaultValue("ChoTiepNhan").IsRequired();
            e.Property(x => x.MaLoiId).HasMaxLength(20);
            e.Property(x => x.HuongXuLy).HasMaxLength(50);
            e.Property(x => x.NguoiDuyetId).HasMaxLength(20);
            e.Property(x => x.LyDoTuChoi).HasMaxLength(255);
            e.HasIndex(x => new { x.TrangThaiYeuCau, x.ThoiDiemGui });

            e.HasOne(x => x.ThietBi).WithMany(t => t.YeuCauBaoTris).HasForeignKey(x => x.ThietBiId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiBao).WithMany().HasForeignKey(x => x.NguoiBaoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MucUuTienDeXuat).WithMany().HasForeignKey(x => x.MucUuTienDeXuatId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MucUuTienChinhThuc).WithMany().HasForeignKey(x => x.MucUuTienChinhThucId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MaLoi).WithMany().HasForeignKey(x => x.MaLoiId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.NguoiDuyet).WithMany().HasForeignKey(x => x.NguoiDuyetId).OnDelete(DeleteBehavior.SetNull);
        });

        // 14. MAU_CHECKLIST
        modelBuilder.Entity<MAU_CHECKLIST>(e =>
        {
            e.ToTable("MAU_CHECKLIST");
            e.HasKey(x => x.MauChecklistId);
            e.Property(x => x.MauChecklistId).HasMaxLength(30);
            e.Property(x => x.TenMau).HasMaxLength(150).IsRequired();
            e.Property(x => x.NhomThietBiId).HasMaxLength(30).IsRequired();
            e.Property(x => x.TrangThaiHieuLuc).HasMaxLength(20).HasDefaultValue("DangApDung").IsRequired();

            e.HasOne(x => x.NhomThietBi)
                .WithMany(n => n.MauChecklists)
                .HasForeignKey(x => x.NhomThietBiId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 15. HANG_MUC_CHECKLIST
        modelBuilder.Entity<HANG_MUC_CHECKLIST>(e =>
        {
            e.ToTable("HANG_MUC_CHECKLIST");
            e.HasKey(x => x.HangMucId);
            e.Property(x => x.MauChecklistId).HasMaxLength(30).IsRequired();
            e.Property(x => x.NhomBoPhan).HasMaxLength(100).IsRequired();
            e.Property(x => x.NoiDungCongViec).HasMaxLength(255).IsRequired();
            e.Property(x => x.KieuKetQua).HasMaxLength(20).IsRequired();
            e.Property(x => x.TieuChuanYeuCau).HasMaxLength(255).IsRequired();
            e.Property(x => x.GiaTriMin).HasPrecision(12, 2);
            e.Property(x => x.GiaTriMax).HasPrecision(12, 2);
            e.Property(x => x.DonViDo).HasMaxLength(30);

            e.HasOne(x => x.MauChecklist)
                .WithMany(m => m.HangMucs)
                .HasForeignKey(x => x.MauChecklistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 16. KE_HOACH_BAO_TRI
        modelBuilder.Entity<KE_HOACH_BAO_TRI>(e =>
        {
            e.ToTable("KE_HOACH_BAO_TRI");
            e.HasKey(x => x.KeHoachId);
            e.Property(x => x.KeHoachId).HasMaxLength(30);
            e.Property(x => x.TenKeHoach).HasMaxLength(150).IsRequired();
            e.Property(x => x.ThietBiId).HasMaxLength(30).IsRequired();
            e.Property(x => x.MauChecklistId).HasMaxLength(30).IsRequired();
            e.Property(x => x.DonViChuKy).HasMaxLength(20).IsRequired();
            e.Property(x => x.TrangThaiKeHoach).HasMaxLength(30).HasDefaultValue("DangApDung").IsRequired();
            e.Property(x => x.MoTa).HasMaxLength(255);
            e.HasIndex(x => new { x.NgayDenHanTiepTheo, x.TrangThaiKeHoach });

            e.HasOne(x => x.ThietBi).WithMany(t => t.KeHoachBaoTris).HasForeignKey(x => x.ThietBiId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MauChecklist).WithMany(m => m.KeHoachs).HasForeignKey(x => x.MauChecklistId).OnDelete(DeleteBehavior.Restrict);
        });

        // 17. LENH_BAO_TRI
        modelBuilder.Entity<LENH_BAO_TRI>(e =>
        {
            e.ToTable("LENH_BAO_TRI");
            e.HasKey(x => x.LenhBaoTriId);
            e.Property(x => x.LenhBaoTriId).HasMaxLength(30);
            e.Property(x => x.ThietBiId).HasMaxLength(30).IsRequired();
            e.Property(x => x.LoaiLenh).HasMaxLength(30).IsRequired();
            e.Property(x => x.YeuCauId).HasMaxLength(30);
            e.Property(x => x.KeHoachId).HasMaxLength(30);
            e.Property(x => x.LyDoTamDung).HasMaxLength(255);
            e.Property(x => x.TrangThaiLenh).HasMaxLength(30).HasDefaultValue("MoiTao").IsRequired();
            e.Property(x => x.TinhTrangSauBaoTri).HasMaxLength(30);
            e.Property(x => x.NguoiBanGiaoId).HasMaxLength(20);
            e.Property(x => x.NguoiKyNhanId).HasMaxLength(20);
            e.Property(x => x.NguoiTaoId).HasMaxLength(20);
            e.Property(x => x.NguoiDuyetDongId).HasMaxLength(20);
            e.HasIndex(x => new { x.ThietBiId, x.ThoiGianDuKienBatDau });
            e.HasIndex(x => new { x.TrangThaiLenh, x.MucUuTienId });

            e.HasOne(x => x.ThietBi).WithMany(t => t.LenhBaoTris).HasForeignKey(x => x.ThietBiId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.YeuCau).WithMany().HasForeignKey(x => x.YeuCauId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.KeHoach).WithMany(k => k.LenhBaoTris).HasForeignKey(x => x.KeHoachId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.MucUuTien).WithMany().HasForeignKey(x => x.MucUuTienId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiBanGiao).WithMany().HasForeignKey(x => x.NguoiBanGiaoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.NguoiKyNhan).WithMany().HasForeignKey(x => x.NguoiKyNhanId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.NguoiTao).WithMany().HasForeignKey(x => x.NguoiTaoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.NguoiDuyetDong).WithMany().HasForeignKey(x => x.NguoiDuyetDongId).OnDelete(DeleteBehavior.SetNull);
        });

        // 18. CHUYEN_MON_KTV
        modelBuilder.Entity<CHUYEN_MON_KTV>(e =>
        {
            e.ToTable("CHUYEN_MON_KTV");
            e.HasKey(x => new { x.NhanVienId, x.NhomThietBiId });
            e.Property(x => x.NhanVienId).HasMaxLength(20);
            e.Property(x => x.NhomThietBiId).HasMaxLength(30);
            e.Property(x => x.GhiChu).HasMaxLength(100);

            e.HasOne(x => x.NhanVien).WithMany(n => n.ChuyenMonKtvs).HasForeignKey(x => x.NhanVienId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.NhomThietBi).WithMany(n => n.ChuyenMonKtvs).HasForeignKey(x => x.NhomThietBiId).OnDelete(DeleteBehavior.Restrict);
        });

        // 19. PHAN_CONG_KTV
        modelBuilder.Entity<PHAN_CONG_KTV>(e =>
        {
            e.ToTable("PHAN_CONG_KTV");
            e.HasKey(x => x.PhanCongId);
            e.Property(x => x.LenhBaoTriId).HasMaxLength(30).IsRequired();
            e.Property(x => x.KyThuatVienId).HasMaxLength(20).IsRequired();
            e.Property(x => x.NguoiPhanCongId).HasMaxLength(20).IsRequired();
            e.Property(x => x.VaiTroTrongLenh).HasMaxLength(30).HasDefaultValue("ThoChinh").IsRequired();
            e.Property(x => x.SoGioCongThucTe).HasPrecision(6, 2);
            e.Property(x => x.TrangThaiPhanCong).HasMaxLength(30).HasDefaultValue("DaGiao").IsRequired();

            e.HasOne(x => x.LenhBaoTri).WithMany(l => l.PhanCongKtvs).HasForeignKey(x => x.LenhBaoTriId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.KyThuatVien).WithMany().HasForeignKey(x => x.KyThuatVienId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiPhanCong).WithMany().HasForeignKey(x => x.NguoiPhanCongId).OnDelete(DeleteBehavior.Restrict);
        });

        // 20. KET_QUA_CHECKLIST
        modelBuilder.Entity<KET_QUA_CHECKLIST>(e =>
        {
            e.ToTable("KET_QUA_CHECKLIST");
            e.HasKey(x => x.KetQuaId);
            e.Property(x => x.LenhBaoTriId).HasMaxLength(30).IsRequired();
            e.Property(x => x.GiaTriDoThucTe).HasPrecision(12, 2);
            e.Property(x => x.KetLuan).HasMaxLength(20).IsRequired();
            e.Property(x => x.NguoiKiemTraId).HasMaxLength(20).IsRequired();
            e.Property(x => x.GhiChu).HasMaxLength(255);

            e.HasOne(x => x.LenhBaoTri).WithMany(l => l.KetQuaChecklists).HasForeignKey(x => x.LenhBaoTriId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.HangMuc).WithMany(h => h.KetQuas).HasForeignKey(x => x.HangMucId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiKiemTra).WithMany().HasForeignKey(x => x.NguoiKiemTraId).OnDelete(DeleteBehavior.Restrict);
        });

        // 21. BAO_CAO_SU_CO
        modelBuilder.Entity<BAO_CAO_SU_CO>(e =>
        {
            e.ToTable("BAO_CAO_SU_CO");
            e.HasKey(x => x.BaoCaoSuCoId);
            e.Property(x => x.LenhBaoTriId).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.LenhBaoTriId).IsUnique();
            e.Property(x => x.MaLoiChinhThucId).HasMaxLength(20).IsRequired();
            e.Property(x => x.NguoiLapBaoCaoId).HasMaxLength(20).IsRequired();

            e.HasOne(x => x.LenhBaoTri).WithOne(l => l.BaoCaoSuCo).HasForeignKey<BAO_CAO_SU_CO>(x => x.LenhBaoTriId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MaLoiChinhThuc).WithMany().HasForeignKey(x => x.MaLoiChinhThucId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiLapBaoCao).WithMany().HasForeignKey(x => x.NguoiLapBaoCaoId).OnDelete(DeleteBehavior.Restrict);
        });

        // 22. PHU_TUNG
        modelBuilder.Entity<PHU_TUNG>(e =>
        {
            e.ToTable("PHU_TUNG");
            e.HasKey(x => x.PhuTungId);
            e.Property(x => x.PhuTungId).HasMaxLength(30);
            e.Property(x => x.TenPhuTung).HasMaxLength(150).IsRequired();
            e.Property(x => x.LoaiPhuTung).HasMaxLength(50).IsRequired();
            e.Property(x => x.QuyCach).HasMaxLength(150).IsRequired();
            e.Property(x => x.DonViTinh).HasMaxLength(20).IsRequired();
            e.Property(x => x.SoLuongTonHienCo).HasPrecision(10, 2);
            e.Property(x => x.SoLuongDangDuTru).HasPrecision(10, 2);
            e.Property(x => x.MucTonToiThieu).HasPrecision(10, 2);
            e.Property(x => x.MoTa).HasMaxLength(255);
        });

        // 23. DINH_MUC_LINH_KIEN
        modelBuilder.Entity<DINH_MUC_LINH_KIEN>(e =>
        {
            e.ToTable("DINH_MUC_LINH_KIEN");
            e.HasKey(x => x.DinhMucId);
            e.Property(x => x.ThietBiId).HasMaxLength(30).IsRequired();
            e.Property(x => x.PhuTungId).HasMaxLength(30).IsRequired();
            e.Property(x => x.SoLuongDinhMuc).HasPrecision(10, 2);
            e.Property(x => x.ViTriLapDat).HasMaxLength(100);
            e.HasIndex(x => new { x.ThietBiId, x.PhuTungId }).IsUnique();

            e.HasOne(x => x.ThietBi).WithMany(t => t.DinhMucLinhKiens).HasForeignKey(x => x.ThietBiId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PhuTung).WithMany(p => p.DinhMucLinhKiens).HasForeignKey(x => x.PhuTungId).OnDelete(DeleteBehavior.Restrict);
        });

        // 24. PHIEU_CAP_NHAT_TON_KHO
        modelBuilder.Entity<PHIEU_CAP_NHAT_TON_KHO>(e =>
        {
            e.ToTable("PHIEU_CAP_NHAT_TON_KHO");
            e.HasKey(x => x.PhieuCapNhatId);
            e.Property(x => x.PhieuCapNhatId).HasMaxLength(30);
            e.Property(x => x.LoaiCapNhat).HasMaxLength(30).IsRequired();
            e.Property(x => x.MaPhieuKhoNgoai).HasMaxLength(50);
            e.Property(x => x.NguoiLapId).HasMaxLength(20).IsRequired();
            e.Property(x => x.NguoiXacNhanId).HasMaxLength(20);
            e.Property(x => x.TrangThaiPhieu).HasMaxLength(30).HasDefaultValue("ChoXacNhan").IsRequired();
            e.Property(x => x.GhiChu).HasMaxLength(255);

            e.HasOne(x => x.NguoiLap).WithMany().HasForeignKey(x => x.NguoiLapId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiXacNhan).WithMany().HasForeignKey(x => x.NguoiXacNhanId).OnDelete(DeleteBehavior.SetNull);
        });

        // 25. CHI_TIET_CAP_NHAT_TON_KHO
        modelBuilder.Entity<CHI_TIET_CAP_NHAT_TON_KHO>(e =>
        {
            e.ToTable("CHI_TIET_CAP_NHAT_TON_KHO");
            e.HasKey(x => x.ChiTietCapNhatId);
            e.Property(x => x.PhieuCapNhatId).HasMaxLength(30).IsRequired();
            e.Property(x => x.PhuTungId).HasMaxLength(30).IsRequired();
            e.Property(x => x.SoLuongThayDoi).HasPrecision(10, 2);
            e.Property(x => x.GhiChu).HasMaxLength(255);

            e.HasOne(x => x.PhieuCapNhat).WithMany(p => p.ChiTiets).HasForeignKey(x => x.PhieuCapNhatId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PhuTung).WithMany(p => p.ChiTietCapNhatTonKhos).HasForeignKey(x => x.PhuTungId).OnDelete(DeleteBehavior.Restrict);
        });

        // 26. PHIEU_DU_TRU_XUAT_DUNG
        modelBuilder.Entity<PHIEU_DU_TRU_XUAT_DUNG>(e =>
        {
            e.ToTable("PHIEU_DU_TRU_XUAT_DUNG");
            e.HasKey(x => x.PhieuXuatId);
            e.Property(x => x.PhieuXuatId).HasMaxLength(30);
            e.Property(x => x.LenhBaoTriId).HasMaxLength(30).IsRequired();
            e.Property(x => x.NguoiLapDuTruId).HasMaxLength(20).IsRequired();
            e.Property(x => x.NguoiKhaiBaoId).HasMaxLength(20);
            e.Property(x => x.NguoiDuyetVuotId).HasMaxLength(20);
            e.Property(x => x.NguoiXacNhanXuatId).HasMaxLength(20);
            e.Property(x => x.TrangThaiPhieu).HasMaxLength(30).HasDefaultValue("DangDuTru").IsRequired();

            e.HasOne(x => x.LenhBaoTri).WithMany(l => l.PhieuDuTruXuatDungs).HasForeignKey(x => x.LenhBaoTriId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiLapDuTru).WithMany().HasForeignKey(x => x.NguoiLapDuTruId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiKhaiBao).WithMany().HasForeignKey(x => x.NguoiKhaiBaoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.NguoiDuyetVuot).WithMany().HasForeignKey(x => x.NguoiDuyetVuotId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.NguoiXacNhanXuat).WithMany().HasForeignKey(x => x.NguoiXacNhanXuatId).OnDelete(DeleteBehavior.SetNull);
        });

        // 27. CHI_TIET_PHIEU_XUAT
        modelBuilder.Entity<CHI_TIET_PHIEU_XUAT>(e =>
        {
            e.ToTable("CHI_TIET_PHIEU_XUAT");
            e.HasKey(x => x.ChiTietId);
            e.Property(x => x.PhieuXuatId).HasMaxLength(30).IsRequired();
            e.Property(x => x.PhuTungId).HasMaxLength(30).IsRequired();
            e.Property(x => x.SoLuongDuTru).HasPrecision(10, 2);
            e.Property(x => x.SoLuongThucDung).HasPrecision(10, 2);
            e.Property(x => x.GhiChuGiaiTrinh).HasMaxLength(255);

            e.HasOne(x => x.PhieuXuat).WithMany(p => p.ChiTiets).HasForeignKey(x => x.PhieuXuatId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PhuTung).WithMany(p => p.ChiTietPhieuXuats).HasForeignKey(x => x.PhuTungId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
