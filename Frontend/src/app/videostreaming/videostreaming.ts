import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Studentservice } from '../Service/StudentService/studentservice';

@Component({
  selector: 'app-videostreaming',
  imports: [],
  templateUrl: './videostreaming.html',
  styleUrl: './videostreaming.css',
})
export class Videostreaming {

   courseId!: number ;

  videos:any[] = [];

  selectedVideo:any;

  constructor(
    private route:ActivatedRoute,
    private service:Studentservice
  ){}
  
   ngOnInit(): void {

    this.route.queryParams.subscribe(params=>{

      this.courseId = params['id'];

      console.log(this.courseId);
    });
  }

    loadVideos(){

    this.service.getCourseVideos(this.courseId).subscribe({

      next:(res:any)=>{

        this.videos = res;

        if(this.videos.length > 0){
          this.selectedVideo = this.videos[0];
        }

      },

      error:(err)=>{
        console.log(err);
      }

    })

  }

  playVideo(video:any){
    this.selectedVideo = video;
  }

}
