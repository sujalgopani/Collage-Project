import { ChangeDetectorRef, Component } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Studentservice } from '../Service/StudentService/studentservice';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-videostreaming',
  imports:[CommonModule, RouterLink],
  templateUrl: './videostreaming.html',
  styleUrls: ['./videostreaming.css'],
})
export class Videostreaming {
  courseId!: number;
  videos: any[] = [];
  selectedVideo: any;
  accessGranted: boolean | null = null;
  course: any = null;
  message: string = ''; // to show friendly error

  constructor(
    private route: ActivatedRoute,
    private service: Studentservice,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe((params) => {
      this.courseId = params['id'];
      this.GetCheckAccess(this.courseId);
    });
  }

  GetCheckAccess(courseId: number) {
    this.accessGranted = null; // loading
    this.course = null;
    this.message = '';

    this.service.GetCheckAccess(courseId).subscribe({
      next: (res) => {
        this.accessGranted = true;

        // fetch course details
        this.service.GetCourseById(courseId).subscribe({
          next: (courseRes) => {
            this.course = courseRes;

            // fetch videos
            this.service.GetCourseVideoById(courseId).subscribe({
              next: (videoRes: any[]) => {
                this.videos = videoRes;
                if (this.videos.length) {
                  this.selectedVideo = this.videos[0]; // auto-select first
                }
                this.cdr.detectChanges();
              },
              error: (err) => {
                if (err.status === 400) {
                  // course not started yet
                  const startDate = this.course?.startDate
                    ? new Date(this.course.startDate)
                    : null;
                  const startDateStr = startDate
                    ? startDate.toLocaleDateString()
                    : 'unknown';
                  this.message = `Course has not started yet. Starting date: ${startDateStr}`;
                } else {
                  this.message = 'Error fetching videos.';
                     console.error('Video fetch error:', err); // optional

                }
                this.cdr.detectChanges();
              },
            });

            this.cdr.detectChanges();
          },
          error: (err) => {
            //console.log(err);
            this.cdr.detectChanges();
          },
        });
      },
      error: (err) => {
      //  console.log(err);
        this.accessGranted = false;
        this.cdr.detectChanges();
      },
    });
  }
}
