import { ChangeDetectorRef, Component, ElementRef, ViewChild } from '@angular/core';
import { Adminservice } from '../Service/AdminService/adminservice';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';

declare var bootstrap: any;

@Component({
  selector: 'app-manage-exmas',
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-exmas.html',
  styleUrl: './manage-exmas.css',
})
export class ManageExmas {
  allExams: any[] = [];
  exams: any[] = [];
  students: any[] = [];
  searchTerm = '';

  selectedExam: any = null;
  selectedExamId: number | null = null;

  errorMessage: string | null = null;

  // ✅ LOADERS
  isLoading = false;
  isDeleting = false;
  isStudentsLoading = false;

  @ViewChild('detailModal') detailModal!: ElementRef;
  @ViewChild('deleteModal') deleteModal!: ElementRef;
  @ViewChild('studentsModal') studentsModal!: ElementRef;

  constructor(
    private service: Adminservice,
    private cd: ChangeDetectorRef,
    private route: ActivatedRoute,
  ) {}

  ngOnInit() {
    this.route.queryParamMap.subscribe((params) => {
      this.searchTerm = (params.get('search') ?? '').trim();
      this.loadExams(this.searchTerm);
    });
  }

  onSearch() {
    this.exams = this.applyExamFilter(this.allExams, this.searchTerm);
  }

  clearSearch() {
    this.searchTerm = '';
    this.exams = this.applyExamFilter(this.allExams, this.searchTerm);
  }

  loadExams(search = '') {
    this.isLoading = true;
    this.errorMessage = null;

    this.service.getAllExams().subscribe({
      next: (res) => {
        this.allExams = res;
        this.exams = this.applyExamFilter(this.allExams, search);
        this.isLoading = false;
        this.cd.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Failed to load exams';
        this.isLoading = false;
        this.cd.detectChanges();
      },
    });
  }

  private applyExamFilter(exams: any[], search: string) {
    const term = search.trim().toLowerCase();
    if (!term) {
      return exams;
    }

    const parsed = Number(term);
    const isNumeric = Number.isFinite(parsed);

    return exams.filter((exam: any) => {
      const title = String(exam?.title ?? '').toLowerCase();
      const description = String(exam?.description ?? '').toLowerCase();
      const examId = Number(exam?.examId);

      return (
        (isNumeric && Number.isFinite(examId) && examId === parsed) ||
        title.includes(term) ||
        description.includes(term)
      );
    });
  }

  // 📘 DETAILS
  openDetailModal(exam: any) {
    this.selectedExam = exam;
    new bootstrap.Modal(this.detailModal.nativeElement).show();
  }

  // 🗑 DELETE
  openDeleteModal(id: number) {
    this.selectedExamId = id;
    new bootstrap.Modal(this.deleteModal.nativeElement).show();
  }

  confirmDelete() {
    if (!this.selectedExamId) return;

    this.isDeleting = true;

    this.service.deleteExam(this.selectedExamId).subscribe({
      next: () => {
        // ✅ remove from FULL list
        this.allExams = this.allExams.filter((e) => e.examId !== this.selectedExamId);
        // ✅ re-apply filter for UI list
        this.exams = this.applyExamFilter(this.allExams, this.searchTerm);
        this.selectedExamId = null;
        this.isDeleting = false;
        this.loadExams(this.searchTerm); // 🔥 BEST REFRESH METHOD
        this.cd.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Delete failed';
        this.isDeleting = false;
        this.cd.detectChanges();
      },
    });
  }
  // 👨‍🎓 STUDENTS
  openStudentsModal(id: number) {
    this.students = [];
    this.isStudentsLoading = true;

    new bootstrap.Modal(this.studentsModal.nativeElement).show();

    this.service.getStudentsByExam(id).subscribe({
      next: (res) => {
        this.students = res;
        this.isStudentsLoading = false;
        this.cd.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Failed to load students';
        this.isStudentsLoading = false;
        this.cd.detectChanges();
      },
    });
  }
}
