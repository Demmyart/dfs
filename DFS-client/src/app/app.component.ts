import { Component } from '@angular/core';
import { RequestService } from './request.service';
import { HttpEventType, HttpResponse } from '@angular/common/http';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})

export class AppComponent {
  title = 'DFS-client';
  fileToUpload: File;
  listDirectories: Array<any> = [];
  path: string = '/';
  dirArr: any[] = [];
  user: any;
  allowed: any;
  searchText: string;
  logsArr: any[] = [];
  contextmenu = false;
  contextmenuX = 0;
  contextmenuY = 0;
  emptyInput: any;
  selectedItem: any;

  constructor(private http: RequestService) { }

  ngOnInit() {
    let modal = document.getElementById('myModal');
    let modal2 = document.getElementById('myModal2');
    let modal3 = document.getElementById('myModal3');
    let modal4 = document.getElementById('myModal4');
    let modal5 = document.getElementById('myModal5');
    let cancel = document.getElementById('cancel');
    let cancel2 = document.getElementById('cancel2');
    let cancel3 = document.getElementById('cancel3');
    let cancel4 = document.getElementById('cancel4');

    cancel.onclick = function () {
      modal.style.display = "none";
    }
    cancel2.onclick = function () {
      modal3.style.display = "none";
    }
    cancel3.onclick = function () {
      modal4.style.display = "none";
    }
    cancel4.onclick = function () {
      modal5.style.display = "none";
    }
    window.onclick = function (event) {
      if (event.target == modal) {
        modal.style.display = "none";
      }
      if (event.target == modal2) {
        modal2.style.display = "none";
      }
      if (event.target == modal3) {
        modal3.style.display = "none";
      }
      if (event.target == modal4) {
        modal4.style.display = "none";
      }
      if (event.target == modal4) {
        modal4.style.display = "none";
      }
    }

  }

  off() {
    // console.log(this.user);
    document.getElementById("overlay").style.display = "none";
    this.http.authUser(this.user).subscribe(data => {
      // console.log(data);
      this.http.getDirectories(this.path, this.user).subscribe(data => {
        this.listDirectories.push(...data.items);
        // console.log("init list", data);
      });
    })
  }

  // 
  // 
  // 
  // 
  //   
  // getDirectories(path) {
  //   console.log(this.user);
  //   this.http.getDirectories(path,this.user).subscribe(data => {
  //     this.listDirectories.push(...data.items);
  //     console.log("init list", data);
  //   });
  //   // console.log("Directories", this.listDirectories);
  // }

  addToPath(newDir) {
    let path = "/";
    this.dirArr.forEach(i => {
      path += i + "/";
    });
    return path + newDir;
  }

  removeFromPath() {
    let path = "";
    if (this.dirArr.length - 1 > 0) {
      for (let i = 0; i < this.dirArr.length - 1; i++) {
        path += "/" + this.dirArr[i];
      };
    } else { path = "/" }
    return path;
  }

  formPath() {
    let path = "";
    if (this.dirArr.length != 0) {
      this.dirArr.forEach(i => {
        path += "/" + i;
      });
    }
    else { path = "/" }
    return path;
  }

  action(file) {
    if (file.type == 'dir') {
      this.http.getDirectories(this.addToPath(file.name), this.user).subscribe(data => {
        this.listDirectories.length = 0;
        this.dirArr.push(file.name);
        this.listDirectories.push(...data.items);
        // console.log("redirect list", data);
      });
    }
    else {
      this.http.downloadFile(file.address, file.reserveAddress, this.formPath() + '/' + file.name, this.user).subscribe(data => {
        // console.log(data);
        let dataType = data.type;
        let binaryData = [];
        binaryData.push(data);
        let downloadLink = document.createElement('a');
        downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: dataType }));
        downloadLink.setAttribute('download', file.name);
        document.body.appendChild(downloadLink);
        downloadLink.click();
        downloadLink.parentNode.removeChild(downloadLink);
      });
    }
  }
  getLogs() {
    document.getElementById('myModal5').style.display = "block";
    this.http.downloadLogs(this.user).subscribe(data => {
      // console.log(data);
      this.logsArr = data.items;

    })
  }

  backTo() {
    this.http.getDirectories(this.removeFromPath(), this.user).subscribe(data => {
      this.listDirectories.length = 0;
      this.dirArr.pop();
      this.listDirectories.push(...data.items);
    });
  }

  backToRoot() {
    this.http.getDirectories(this.path, this.user).subscribe(data => {
      this.listDirectories.length = 0;
      this.dirArr.length = 0;
      this.listDirectories.push(...data.items);
    });
  }

  openInput() {
    document.getElementById('myModal2').style.display = "block";
  }

  openDelete() {
    document.getElementById('myModal3').style.display = "block";
  }

  onrightClick(event, dir) {
    this.selectedItem = dir;
    this.contextmenuX = event.clientX;
    this.contextmenuY = event.clientY;
    this.contextmenu = true;
  }

  disableContextMenu() {
    this.contextmenu = false;
  }

  delteItem() {
    document.getElementById('myModal3').style.display = "none";
    if (this.selectedItem.type == 'dir') {
      this.http.deleteDirectory(this.formPath() + '&name=' + this.selectedItem.name, this.user).subscribe(data => {
        // console.log(data);
        this.http.getDirectories(this.formPath(), this.user).subscribe(data => {
          this.listDirectories.length = 0;
          this.listDirectories.push(...data.items);
        })
      })
    }
    else {
      this.http.deleteFile(this.formPath() + '&name=' + this.selectedItem.name, this.user).subscribe(data => {
        // console.log(data);
        this.http.getDirectories(this.formPath(), this.user).subscribe(data => {
          this.listDirectories.length = 0;
          this.listDirectories.push(...data.items);
        })
      })
    }
  }

  openInfo() {
    document.getElementById('myModal4').style.display = "block";
    let message: HTMLElement = document.getElementsByClassName("file_info")[0] as HTMLElement;
    message.innerHTML = "<p>Size: " + this.selectedItem.size + "</p>" + "<p>Address: " + this.selectedItem.address + "</p>";

  }

  duplicate() {
    // console.log("File to upload", this.fileToUpload[0]);
    let result = this.listDirectories.map(a => a.name);
    var name = this.fileToUpload[0].name;
    let counter = 1;
    while (result.includes(name + ' (' + counter.toString() + ')')) {
      counter += 1;
    }
    name += ' (' + counter.toString() + ')';
    document.getElementById('myModal').style.display = 'none';
    if (this.fileToUpload != null) {
      this.http.uploadFileNameServer(this.fileToUpload[0].size, this.user).subscribe(data => {
        document.getElementById("sliderbar").style.width = 0 + '%';
        document.getElementById("szazalek").innerHTML = 0 + '%';
        this.http.uploadFileStorage(data.mainStorage, this.fileToUpload[0], this.formPath(), name, this.user).subscribe(event => {
          if (event.type === HttpEventType.UploadProgress) {
            const percentDone = Math.round(100 * event.loaded / event.total);
            // console.log(`File is ${percentDone}% uploaded.`);
            document.getElementById("sliderbar").style.width = percentDone + '%';
            document.getElementById("szazalek").innerHTML = percentDone + '%';
          } else if (event instanceof HttpResponse) {
            console.log('File is completely uploaded!');
          }
          this.http.getDirectories(this.formPath(), this.user).subscribe(data => {
            this.listDirectories.length = 0;
            this.listDirectories.push(...data.items);
            // console.log(data);
          });
        });
        // console.log(data.mainStorage);
      })
      let message: HTMLElement = document.getElementsByClassName("file-msg")[0] as HTMLElement;
      message.innerHTML = this.fileToUpload[0].name + ' successfully uploaded';
    }
  }

  rewrite() {
    // console.log("File to upload", this.fileToUpload[0]);
    document.getElementById('myModal').style.display = 'none';
    if (this.fileToUpload != null) {
      this.http.deleteFile(this.formPath() + '&name=' + this.fileToUpload[0].name, this.user).subscribe(data => {
        this.http.uploadFileNameServer(this.fileToUpload[0].size, this.user).subscribe(data => {
          document.getElementById("sliderbar").style.width = 0 + '%';
          document.getElementById("szazalek").innerHTML = 0 + '%';
          this.http.uploadFileStorage(data.mainStorage, this.fileToUpload[0], this.formPath(), this.fileToUpload[0].name, this.user).subscribe(event => {
            if (event.type === HttpEventType.UploadProgress) {
              const percentDone = Math.round(100 * event.loaded / event.total);
              // console.log(`File is ${percentDone}% uploaded.`);
              document.getElementById("sliderbar").style.width = percentDone + '%';
              document.getElementById("szazalek").innerHTML = percentDone + '%';
            } else if (event instanceof HttpResponse) {
              console.log('File is completely uploaded!');
            }
            this.http.getDirectories(this.formPath(), this.user).subscribe(data => {
              this.listDirectories.length = 0;
              this.listDirectories.push(...data.items);
              // console.log(data);
            });
          });
          // console.log(data.mainStorage);
        })

        this.http.getDirectories(this.formPath(), this.user).subscribe(data => {
          this.listDirectories.length = 0;
          this.listDirectories.push(...data.items);
        })
      })
      let message: HTMLElement = document.getElementsByClassName("file-msg")[0] as HTMLElement;
      message.innerHTML = this.fileToUpload[0].name + ' successfully uploaded';
    }
  }

  createDir(path) {
    document.getElementById('myModal2').style.display = "none";
    this.http.createDirectory(this.formPath() + '&name=' + path, this.user).subscribe(data => {
      // console.log(data);
      this.http.getDirectories(this.formPath(), this.user).subscribe(data => {
        this.listDirectories.length = 0;
        this.listDirectories.push(...data.items);
      })
    })
  }

  onDrop(event: any) {
    event.preventDefault();
    event.stopPropagation();
    this.fileToUpload = event.dataTransfer.files;
    let result = this.listDirectories.map(a => a.name);
    // console.log("names", result);
    if (result.includes(event.dataTransfer.files[0].name)) {
      document.getElementById('myModal').style.display = "block";
    }
    else {
      if (this.fileToUpload != null) {
        this.http.uploadFileNameServer(this.fileToUpload[0].size, this.user).subscribe(data => {
          document.getElementById("sliderbar").style.width = 0 + '%';
          document.getElementById("szazalek").innerHTML = 0 + '%';
          this.http.uploadFileStorage(data.mainStorage, this.fileToUpload[0], this.formPath(), this.fileToUpload[0].name, this.user).subscribe(event => {
            if (event.type === HttpEventType.UploadProgress) {
              const percentDone = Math.round(100 * event.loaded / event.total);
              // console.log(`File is ${percentDone}% uploaded.`);
              document.getElementById("sliderbar").style.width = percentDone + '%';
              document.getElementById("szazalek").innerHTML = percentDone + '%';
            } else if (event instanceof HttpResponse) {
              console.log('File is completely uploaded!');
            }
            this.http.getDirectories(this.formPath(), this.user).subscribe(data => {
              this.listDirectories.length = 0;
              this.listDirectories.push(...data.items);
            });
          });
          // console.log(data.mainStorage);
        })
        // console.log("File to upload", event.dataTransfer.files[0]);
        let message: HTMLElement = document.getElementsByClassName("file-msg")[0] as HTMLElement;
        message.innerHTML = this.fileToUpload[0].name + ' successfully uploaded';
      }
    }
  }

  onDragOver(evt) {
    evt.preventDefault();
    evt.stopPropagation();
    let fileInput: HTMLElement = document.getElementsByClassName("file-drop-area")[0] as HTMLElement;
    fileInput.classList.add('is-active');
  }

  onDragLeave(evt) {
    evt.preventDefault();
    evt.stopPropagation();
    let fileInput: HTMLElement = document.getElementsByClassName("file-drop-area")[0] as HTMLElement;
    fileInput.classList.remove('is-active');
  }

  onFileChange(event) {
    this.fileToUpload = event.target.files;
    let result = this.listDirectories.map(a => a.name);
    if (result.includes(event.target.files[0].name)) {
      document.getElementById('myModal').style.display = "block";
    }
    else {
      if (this.fileToUpload != null) {
        this.http.uploadFileNameServer(this.fileToUpload[0].size, this.user).subscribe(data => {
          document.getElementById("sliderbar").style.width = 0 + '%';
          document.getElementById("szazalek").innerHTML = 0 + '%';
          this.http.uploadFileStorage(data.mainStorage, this.fileToUpload[0], this.formPath(), this.fileToUpload[0].name, this.user).subscribe(event => {
            if (event.type === HttpEventType.UploadProgress) {
              const percentDone = Math.round(100 * event.loaded / event.total);
              // console.log(`File is ${percentDone}% uploaded.`);
              document.getElementById("sliderbar").style.width = percentDone + '%';
              document.getElementById("szazalek").innerHTML = percentDone + '%';
            } else if (event instanceof HttpResponse) {
              console.log('File is completely uploaded!');
            }
            this.http.getDirectories(this.formPath(), this.user).subscribe(data => {
              this.listDirectories.length = 0;
              this.listDirectories.push(...data.items);
            });
          });
          // console.log(data.mainStorage);
        });
      }
      // console.log("File to upload", event.target.files[0]);
      let message: HTMLElement = document.getElementsByClassName("file-msg")[0] as HTMLElement;
      message.innerHTML = this.fileToUpload[0].name + ' successfully uploaded';
    }
  }

}
