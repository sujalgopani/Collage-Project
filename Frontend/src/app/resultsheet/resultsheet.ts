import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Studentservice } from '../Service/StudentService/studentservice';

@Component({
  selector: 'app-resultsheet',
  imports: [CommonModule],
  templateUrl: './resultsheet.html',
  styleUrl: './resultsheet.css',
})
export class Resultsheet implements OnInit {
  constructor(
    private router: ActivatedRoute,
    private service: Studentservice,
    private cd: ChangeDetectorRef,
    private navigate: Router,
  ) {}

  resultdata: any;
  percentage = 0;
  grade = '';
  today = new Date();
  isDownloading = false;

  ngOnInit(): void {
    const examId = Number(this.router.snapshot.paramMap.get('examId'));
    const attemptId = Number(this.router.snapshot.paramMap.get('attemptId'));

    this.service.GetExamResult(examId, attemptId).subscribe({
      next: (res: any) => {
        console.log(res);
        this.resultdata = res;
        // calculate percentage
        this.percentage = (res.totalScore / res.maxScore) * 100;

        // grade logic
        if (this.percentage >= 90) this.grade = 'A+';
        else if (this.percentage >= 80) this.grade = 'A';
        else if (this.percentage >= 70) this.grade = 'B';
        else if (this.percentage >= 60) this.grade = 'C';
        else if (this.percentage >= 50) this.grade = 'D';
        else this.grade = 'F';
        this.cd.detectChanges();
      },
      error: (err) => {
        console.log(err);
        this.cd.detectChanges();
      },
    });
  }

  backbtn() {
    this.navigate.navigate(['/student-dashboard/student-exam-result']);
  }

  async downloadResultPdf() {
    if (this.isDownloading) return;

    const resultSheet = document.getElementById('resultSheet');
    if (!resultSheet) return;

    this.isDownloading = true;

    try {
      const [html2canvasModule, jsPdfModule] = await Promise.all([
        import('html2canvas'),
        import('jspdf'),
      ]);
      const html2canvas = html2canvasModule.default;
      const JsPDF = jsPdfModule.jsPDF;

      const canvas = await html2canvas(resultSheet, {
        scale: 2,
        useCORS: true,
        backgroundColor: '#ffffff',
      });

      const imageData = canvas.toDataURL('image/png');
      const pdf = new JsPDF('p', 'mm', 'a4');
      const pdfWidth = pdf.internal.pageSize.getWidth();
      const pdfHeight = pdf.internal.pageSize.getHeight();
      const imageHeight = (canvas.height * pdfWidth) / canvas.width;

      let heightLeft = imageHeight;
      let position = 0;

      pdf.addImage(imageData, 'PNG', 0, position, pdfWidth, imageHeight, '', 'FAST');
      heightLeft -= pdfHeight;

      while (heightLeft > 0) {
        position = heightLeft - imageHeight;
        pdf.addPage();
        pdf.addImage(imageData, 'PNG', 0, position, pdfWidth, imageHeight, '', 'FAST');
        heightLeft -= pdfHeight;
      }

      const safeExam = String(this.resultdata?.examTitle || 'Exam').replace(/[^a-zA-Z0-9]+/g, '_');
      const safeStudent = String(this.resultdata?.username || 'Student').replace(
        /[^a-zA-Z0-9]+/g,
        '_',
      );

      pdf.save(`${safeStudent}_${safeExam}_Result.pdf`);
    } catch (err) {
      console.error('Result PDF download failed', err);
    } finally {
      this.isDownloading = false;
      this.cd.detectChanges();
    }
  }

  
}
