import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PaymentService {
  constructor(private http: HttpClient) {}

  createOrder(data:any){
    return this.http.post("https://localhost:44385/api/Payment/create-order",data);
  }

  verifyPayment(data:any){
    return this.http.post("https://localhost:44385/api/Payment/verify-payment",data);
  }

}
