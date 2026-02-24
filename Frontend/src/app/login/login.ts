import { Component, OnInit } from '@angular/core';
import { isEarlyEventType } from '@angular/core/primitives/event-dispatch';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit {
  loginfrm!: FormGroup;
  Isseen = false  ;

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.loginfrm =this.fb.group({
      username: ['', [Validators.required, Validators.minLength(5)]],
      password: ['', Validators.required],
    });
  }

  toggleSeen(passwordInput: HTMLInputElement){
    this.Isseen = !this.Isseen;
    setTimeout(() => {
    passwordInput.focus();
  }, 0);
  }

  Onlogin() {
    console.log('logom');
  }
}
