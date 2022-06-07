#lang racket/gui
(require file/zip)
(require file/sha1)

(define payload-dir
  (get-directory "Select payload directory."))

(define (create-payload)
  (current-directory payload-dir)
  (when (file-exists? "../payload.zip")
    (delete-file "../payload.zip"))
  
  (when (not (directory-exists? "../payload"))
    (make-directory "../payload"))
  
  (map (lambda (f)
         (display (string-append "../payload/" (path->string f) "\n"))
         (copy-directory/files f (string-append "../payload/" (path->string f))))
       (directory-list))
  
  (current-directory "../")
  (zip "payload.zip" "payload")
  (delete-directory/files "payload"))

(define (sign-payload)
  (when (file-exists? "payload.zip.sig")
    (delete-file "payload.zip.sig"))
  (system "gpg --detach-sign --default-key support@fluxinc.ca payload.zip"))

(define (create-update-file)
  (let* ((sha256-hash (call-with-input-file "payload.zip"
                        (lambda (in)
                          (bytes->hex-string
                           (sha256-bytes in)))))
         (final-name (string-append "warden-" sha256-hash))
         (zipped-name (string-append final-name ".zip")))
    
    (when (not (directory-exists? final-name))
      (make-directory final-name))

    (when (file-exists? zipped-name)
      (delete-file zipped-name))

    (map (lambda (f) (rename-file-or-directory f (string-append final-name "/" f)))
         '("payload.zip" "payload.zip.sig"))
    (zip zipped-name final-name)
    (delete-directory/files final-name)))

(define (warden-create)
  (display "Creating Payload.zip\n")
  (create-payload)
  (display "Signing Payload.zip\n")
  (sign-payload)
  (display "Creating update file\n")
  (create-update-file)
  (display "Completed. Please any key to quit.")
  (read-line)
  (exit))

(warden-create)

