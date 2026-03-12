import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormBuilder } from '@angular/forms';

@Component({
  selector: 'app-teacher-profile',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './teacher-profile.html',
  styleUrl: './teacher-profile.css',
})
export class TeacherProfile {
isEditMode = false;

  profileForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.profileForm = this.fb.group({
      name: ['Hit Dungrani'],
      email: ['teacher@gmail.com'],
      phone: ['9876543210'],
      experience: ['5 Years'],
      skills: ['Angular, .NET, SQL'],
      bio: ['Passionate educator with experience in modern web technologies.'],
      photo: ['https://via.placeholder.com/150']
    });
  }

  toggleEdit() {
    this.isEditMode = !this.isEditMode;
  }

  saveProfile() {
    console.log(this.profileForm.value);
    this.isEditMode = false;
    alert("Profile Updated Successfully ✅");
  }
}
